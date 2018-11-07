// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FlatMap<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, IAsyncEnumerable<TResult>> _mapper;

        private readonly int _maxConcurrency;

        private readonly int _prefetch;

        public FlatMap(IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper, int maxConcurrency, int prefetch)
        {
            _source = source;
            _mapper = mapper;
            _maxConcurrency = maxConcurrency;
            _prefetch = prefetch;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            var en = new FlatMapEnumerator(_source.GetAsyncEnumerator(), _mapper, _maxConcurrency, _prefetch);
            en.MoveNext();
            return en;
        }

        internal sealed class FlatMapEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, IAsyncEnumerable<TResult>> _mapper;

            private readonly int _prefetch;

            private readonly ConcurrentQueue<Item> _queue;

            private TaskCompletionSource<bool> _resume;

            private InnerHandler[] _inners;

            private volatile bool _done;
            private Exception _errors;

            private static readonly InnerHandler[] Empty = new InnerHandler[0];
            private static readonly InnerHandler[] Terminated = new InnerHandler[0];

            public TResult Current { get; private set; }

            private int _allDisposeWip;
            private Exception _allDisposeError;
            private readonly TaskCompletionSource<bool> _allDisposeTask;

            private int _outstanding;

            private int _sourceWip;

            private int _sourceDisposeWip;

            public FlatMapEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper, int maxConcurrency, int prefetch)
            {
                _source = source;
                _mapper = mapper;
                _queue = new ConcurrentQueue<Item>();
                _prefetch = prefetch;
                _allDisposeWip = 1; // the main source is one
                _allDisposeTask = new TaskCompletionSource<bool>();
                Volatile.Write(ref _outstanding, maxConcurrency);
                Volatile.Write(ref _inners, Empty);
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref _sourceDisposeWip) == 1)
                {
                    Dispose(_source);
                }

                var a = Interlocked.Exchange(ref _inners, Terminated);
                foreach (var handler in a)
                {
                    handler.Dispose();
                }
                return new ValueTask(_allDisposeTask.Task);
            }

            internal void MoveNext()
            {
                QueueDrainHelper.MoveNext(_source, ref _sourceWip, ref _sourceDisposeWip, HandleAction, this);
            }

            private static readonly Action<Task<bool>, object>
                HandleAction = (t, state) => ((FlatMapEnumerator) state).Handle(t);

            private bool TryDispose()
            {
                if (Interlocked.Decrement(ref _sourceDisposeWip) != 0)
                {
                    Dispose(_source);
                    return false;
                }
                return true;
            }

            private void Handle(Task<bool> task)
            {
                if (task.IsFaulted)
                {
                    AddException(task.Exception);
                    _done = true;
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
                else
                {
                    if (task.Result)
                    {
                        var v = _source.Current;

                        if (TryDispose())
                        {
                            IAsyncEnumerator<TResult> innerSource;
                            try
                            {
                                innerSource = _mapper(v)
                                    .GetAsyncEnumerator();
                            }
                            catch (Exception ex)
                            {
                                _source.DisposeAsync();

                                AddException(ex);
                                _done = true;
                                Signal();
                                return;
                            }

                            var handler = new InnerHandler(this, innerSource, _prefetch);
                            Interlocked.Increment(ref _allDisposeWip);
                            if (Add(handler))
                            {
                                handler.MoveNext();

                                if (Interlocked.Decrement(ref _outstanding) != 0)
                                {
                                    MoveNext();
                                }
                            }
                            else
                            {
                                // This will decrement _allDisposeWip so
                                // that the DisposeAsync() can be released eventually
                                DisposeOne();
                            }
                        }
                    }
                    else
                    {
                        _done = true;
                        if (TryDispose())
                        {
                            Signal();
                        }
                    }
                }
            }

            internal void Dispose(IAsyncDisposable disposable)
            {
                disposable.DisposeAsync()
                    .AsTask()
                    .ContinueWith(DisposeHandlerAction, this);
            }

            private static readonly Action<Task, object> DisposeHandlerAction = (t, state) => ((FlatMapEnumerator)state).DisposeHandler(t);

            private void DisposeHandler(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _allDisposeWip, ref _allDisposeError, _allDisposeTask);
            }

            private void DisposeOne()
            {
                QueueDrainHelper.DisposeOne(ref _allDisposeWip, ref _allDisposeError, _allDisposeTask);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    var d = _done && Volatile.Read(ref _inners).Length == 0;
                    var success = _queue.TryDequeue(out var v);

                    if (d && !success)
                    {
                        if (_errors != null)
                        {
                            throw _errors;
                        }
                        return false;
                    }

                    if (success)
                    {
                        if (v.HasValue)
                        {
                            Current = v.Value;
                            v.Sender.ConsumedOne();
                            return true;
                        }

                        Remove(v.Sender);
                        NextSource();
                        continue;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            private bool Add(InnerHandler handler)
            {
                for (; ;)
                {
                    var a = Volatile.Read(ref _inners);
                    if (a == Terminated)
                    {
                        return false;
                    }
                    var n = a.Length;
                    var b = new InnerHandler[n + 1];
                    Array.Copy(a, 0, b, 0, n);
                    b[n] = handler;

                    if (Interlocked.CompareExchange(ref _inners, b, a) == a)
                    {
                        return true;
                    }
                }
            }

            private void Remove(InnerHandler handler)
            {
                for (; ; )
                {
                    var a = Volatile.Read(ref _inners);
                    var n = a.Length;
                    if (n == 0)
                    {
                        break;
                    }
                    var idx = Array.IndexOf(a, handler);
                    if (idx < 0)
                    {
                        break;
                    }
                    InnerHandler[] b;
                    if (n == 1)
                    {
                        b = Empty;
                    }
                    else
                    {
                        b = new InnerHandler[n - 1];
                        Array.Copy(a, 0, b, 0, idx);
                        Array.Copy(a, idx + 1, b, idx, n - idx - 1);
                    }

                    if (Interlocked.CompareExchange(ref _inners, b, a) == a)
                    {
                        handler.Dispose();
                        break;
                    }
                }
            }

            private void AddException(Exception ex)
            {
                ExceptionHelper.AddException(ref _errors, ex);
            }

            private void NextSource()
            {
                if (Interlocked.Increment(ref _outstanding) == 1)
                {
                    MoveNext();
                }
            }

            internal void InnerNext(InnerHandler sender, TResult item)
            {
                _queue.Enqueue(new Item
                {
                    Sender = sender,
                    Value = item,
                    HasValue = true
                });
                Signal();
            }

            internal void InnerError(InnerHandler sender, Exception ex)
            {
                AddException(ex);
                _queue.Enqueue(new Item
                {
                    Sender = sender
                });
                Signal();
            }

            internal void InnerComplete(InnerHandler sender)
            {
                _queue.Enqueue(new Item
                {
                    Sender = sender
                });
                Signal();
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }
        }

        internal sealed class InnerHandler
        {
            private readonly FlatMapEnumerator _parent;

            private readonly IAsyncEnumerator<TResult> _source;

            private readonly int _prefetch;

            private int _dispose;

            private int _wip;

            private int _outstanding;

            private int _consumed;

            public InnerHandler(FlatMapEnumerator parent, IAsyncEnumerator<TResult> source, int prefetch)
            {
                _parent = parent;
                _source = source;
                _prefetch = prefetch;
                Volatile.Write(ref _outstanding, prefetch);
            }

            internal void ConsumedOne()
            {
                var c = _consumed + 1;
                var limit = _prefetch - (_prefetch >> 2);
                if (c == limit)
                {
                    _consumed = 0;
                    if (Interlocked.Add(ref _outstanding, limit) == limit)
                    {
                        MoveNext();
                    }
                }
                else
                {
                    _consumed = c;
                }
            }

            internal void Dispose()
            {
                if (Interlocked.Increment(ref _dispose) == 1)
                {
                    _parent.Dispose(_source);
                }
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref _wip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref _dispose) == 1)
                        {
                            _source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(HandleAction, this);
                        }
                        else
                        {
                            break;
                        }
                    } while (Interlocked.Decrement(ref _wip) != 0);
                }
            }

            private static readonly Action<Task<bool>, object>
                HandleAction = (t, state) => ((InnerHandler) state).Handle(t);

            private bool TryDispose()
            {
                if (Interlocked.Decrement(ref _dispose) != 0)
                {
                    _parent.Dispose(_source);
                    return false;
                }
                return true;
            }

            private void Handle(Task<bool> task)
            {
                if (task.IsFaulted)
                {
                    if (TryDispose())
                    {
                        _parent.InnerError(this, task.Exception);
                    }
                }
                else
                {
                    if (task.Result)
                    {
                        var v = _source.Current;
                        if (TryDispose())
                        {
                            _parent.InnerNext(this, v);
                            if (Interlocked.Decrement(ref _outstanding) != 0)
                            {
                                MoveNext();
                            }
                        }
                    }
                    else
                    {
                        if (TryDispose())
                        {
                            _parent.InnerComplete(this);
                        }
                    }
                }
            }
        }

        private struct Item
        {
            internal InnerHandler Sender;
            internal TResult Value;
            internal bool HasValue;
        }
    }
}
