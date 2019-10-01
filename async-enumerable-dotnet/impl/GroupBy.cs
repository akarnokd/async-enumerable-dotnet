// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class GroupBy<TSource, TKey, TValue> : IAsyncEnumerable<IAsyncGroupedEnumerable<TKey, TValue>>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, TKey> _keySelector;

        private readonly Func<TSource, TValue> _valueSelector;

        private readonly IEqualityComparer<TKey> _keyComparer;

        public GroupBy(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey> keyComparer)
        {
            _source = source;
            _keySelector = keySelector;
            _valueSelector = valueSelector;
            _keyComparer = keyComparer;
        }

        public IAsyncEnumerator<IAsyncGroupedEnumerable<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new GroupByEnumerator(_source.GetAsyncEnumerator(cancellationToken), _keySelector, _valueSelector, _keyComparer);
        }

        private sealed class GroupByEnumerator : IAsyncEnumerator<IAsyncGroupedEnumerable<TKey, TValue>>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, TKey> _keySelector;

            private readonly Func<TSource, TValue> _valueSelector;

            private readonly ConcurrentDictionary<TKey, Group> _groups;

            private int _active;

            public IAsyncGroupedEnumerable<TKey, TValue> Current { get; private set; }

            private bool _done;

            public GroupByEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey> keyComparer)
            {
                _source = source;
                _keySelector = keySelector;
                _valueSelector = valueSelector;
                _groups = new ConcurrentDictionary<TKey, Group>(keyComparer);
                Volatile.Write(ref _active, 1);
            }

            public async ValueTask DisposeAsync()
            {
                if (!_done)
                {
                    for (; ; )
                    {
                        try
                        {
                            if (_groups.IsEmpty)
                            {
                                break;
                            }
                            if (await _source.MoveNextAsync())
                            {
                                var t = _source.Current;
                                var k = _keySelector(t);

                                var found = _groups.TryGetValue(k, out var g);

                                if (found)
                                {
                                    await g.Next(_valueSelector(t));
                                }
                            }
                            else
                            {
                                foreach (var gr in _groups)
                                {
                                    await gr.Value.Complete();
                                }
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            foreach (var gr in _groups)
                            {
                                await gr.Value.Error(ex);
                            }

                            throw;
                        }
                    }
                }
                if (Interlocked.Decrement(ref _active) == 0)
                {
                    await _source.DisposeAsync();
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    try
                    {
                        if (await _source.MoveNextAsync())
                        {
                            var t = _source.Current;
                            var k = _keySelector(t);

                            var found = _groups.TryGetValue(k, out var g);

                            if (!found)
                            {
                                g = new Group(k, this);
                                Interlocked.Increment(ref _active);
                                _groups.TryAdd(k, g);
                                Current = g;
                            }

                            await g.Next(_valueSelector(t));

                            if (!found)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            foreach (var gr in _groups)
                            {
                                await gr.Value.Complete();
                            }
                            _done = true;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        foreach (var gr in _groups)
                        {
                            await gr.Value.Error(ex);
                        }
                        _done = true;
                        throw;
                    }
                }
            }

            private async ValueTask Remove(Group g)
            {
                _groups.TryRemove(g.Key, out _);
                if (Interlocked.Decrement(ref _active) == 0)
                {
                    await _source.DisposeAsync();
                }
            }

            private sealed class Group : IAsyncGroupedEnumerable<TKey, TValue>, IAsyncEnumerator<TValue>, IAsyncConsumer<TValue>
            {
                private readonly GroupByEnumerator _parent;

                public TKey Key { get; }

                public TValue Current { get; private set; }

                private int _once;

                private TValue _current;
                private Exception _error;
                private bool _done;

                private TaskCompletionSource<bool> _consumed;

                private TaskCompletionSource<bool> _valueReady;

                public Group(TKey key, GroupByEnumerator parent)
                {
                    Key = key;
                    _parent = parent;
                    ResumeHelper.Resume(ref _consumed);
                }

                public async ValueTask Next(TValue value)
                {
                    await ResumeHelper.Await(ref _consumed);
                    ResumeHelper.Clear(ref _consumed);

                    _current = value;

                    ResumeHelper.Resume(ref _valueReady);
                }

                public async ValueTask Error(Exception ex)
                {
                    await ResumeHelper.Await(ref _consumed);
                    ResumeHelper.Clear(ref _consumed);

                    _error = ex;
                    _done = true;

                    ResumeHelper.Resume(ref _valueReady);
                }

                public async ValueTask Complete()
                {
                    await ResumeHelper.Await(ref _consumed);
                    ResumeHelper.Clear(ref _consumed);

                    _done = true;

                    ResumeHelper.Resume(ref _valueReady);
                }


                public IAsyncEnumerator<TValue> GetAsyncEnumerator(CancellationToken cancellationToken)
                {
                    if (Interlocked.CompareExchange(ref _once, 1, 0) == 0)
                    {
                        return this;
                    }
                    return new Error<TValue>.ErrorEnumerator(new InvalidOperationException("Only one IAsyncEnumerator supported"));
                }

                public async ValueTask<bool> MoveNextAsync()
                {
                    await ResumeHelper.Await(ref _valueReady);
                    ResumeHelper.Clear(ref _valueReady);

                    if (_done)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }
                    Current = _current;
                    ResumeHelper.Resume(ref _consumed);
                    return true;
                }

                public async ValueTask DisposeAsync()
                {
                    await _parent.Remove(this);
                    ResumeHelper.Resume(ref _consumed);
                }
            }
        }
    }
}
