// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class CombineLatest<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource>[] _sources;

        private readonly Func<TSource[], TResult> _combiner;

        public CombineLatest(IAsyncEnumerable<TSource>[] sources, Func<TSource[], TResult> combiner)
        {
            _sources = sources;
            _combiner = combiner;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            return new CombineLatestEnumerator(_sources, _combiner);
        }

        private sealed class CombineLatestEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly InnerHandler[] _sources;

            private readonly Func<TSource[], TResult> _combiner;

            public TResult Current { get; private set; }

            private bool _once;

            private TaskCompletionSource<bool> _resume;

            private Exception _disposeError;
            private int _disposeWip;
            private readonly TaskCompletionSource<bool> _disposeTask;

            private Exception _error;
            private int _done;
            private TSource[] _latest;
            private int _latestRemaining;

            private readonly ConcurrentQueue<Entry> _queue;

            public CombineLatestEnumerator(IAsyncEnumerable<TSource>[] sources, Func<TSource[], TResult> combiner)
            {
                var n = sources.Length;
                _sources = new InnerHandler[n];
                for (var i = 0; i < n; i++)
                {
                    _sources[i] = new InnerHandler(sources[i].GetAsyncEnumerator(), this, i);
                }
                _combiner = combiner;
                _disposeTask = new TaskCompletionSource<bool>();
                _latest = new TSource[n];
                _queue = new ConcurrentQueue<Entry>();
                _latestRemaining = n;
                Volatile.Write(ref _disposeWip, n);
                Volatile.Write(ref _done, n);
            }

            private void MoveNextAll()
            {
                foreach (var inner in _sources)
                {
                    inner.MoveNext();
                }
            }

            public async ValueTask DisposeAsync()
            {
                foreach (var inner in _sources)
                {
                    inner.Dispose();
                }
                await _disposeTask.Task;

                _latest = null;
                Current = default;
                while (_queue.TryDequeue(out _)) { }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (!_once)
                {
                    _once = true;
                    MoveNextAll();
                }

                var latest = _latest;
                var n = latest.Length;

                for (; ; ) {

                    if (_done == 0)
                    {
                        var ex = ExceptionHelper.Terminate(ref _error);
                        if (ex != null)
                        {
                            throw ex;
                        }
                        return false;
                    }

                    var success = _queue.TryDequeue(out var entry);

                    if (success)
                    {
                        var inner = _sources[entry.Index];

                        if (entry.Done)
                        {
                            if (inner.HasLatest)
                            {
                                _done--;
                            }
                            else
                            {
                                _done = 0;
                            }
                            continue;
                        }

                        if (!inner.HasLatest)
                        {
                            inner.HasLatest = true;
                            _latestRemaining--;
                        }

                        latest[entry.Index] = entry.Value;

                        if (_latestRemaining == 0)
                        {
                            var copy = new TSource[n];
                            Array.Copy(latest, 0, copy, 0, n);

                            Current = _combiner(copy);

                            inner.MoveNext();
                            return true;
                        }

                        inner.MoveNext();
                        continue;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            private void Dispose(IAsyncDisposable disposable)
            {
                disposable.DisposeAsync()
                    .AsTask()
                    .ContinueWith(DisposeHandlerAction, this);
            }

            private static readonly Action<Task, object> DisposeHandlerAction = (t, state) => ((CombineLatestEnumerator)state).DisposeHandler(t);

            private void DisposeHandler(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _disposeWip, ref _disposeError, _disposeTask);
            }

            private void InnerNext(int index, TSource value)
            {
                _queue.Enqueue(new Entry
                {
                    Index = index,
                    Done = false,
                    Value = value
                });
            }

            private void InnerError(int index, Exception ex)
            {
                ExceptionHelper.AddException(ref _error, ex);
                _queue.Enqueue(new Entry {
                    Index = index, Done = true, Value = default
                });
            }

            private void InnerComplete(int index)
            {
                _queue.Enqueue(new Entry { Index = index, Done = true, Value = default });
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }

            private struct Entry
            {
                internal int Index;
                internal TSource Value;
                internal bool Done;
            }

            private sealed class InnerHandler
            {
                private readonly IAsyncEnumerator<TSource> _source;

                private readonly CombineLatestEnumerator _parent;

                private readonly int _index;

                private int _disposeWip;

                private int _sourceWip;

                internal bool HasLatest;

                public InnerHandler(IAsyncEnumerator<TSource> source, CombineLatestEnumerator parent, int index)
                {
                    _source = source;
                    _parent = parent;
                    _index = index;
                }

                internal void MoveNext()
                {
                    QueueDrainHelper.MoveNext(_source, ref _sourceWip, ref _disposeWip, NextHandlerAction, this);
                }

                private static readonly Action<Task<bool>, object> NextHandlerAction = (t, state) => ((InnerHandler)state).NextHandler(t);

                private bool TryDispose()
                {
                    if (Interlocked.Decrement(ref _disposeWip) != 0)
                    {
                        _parent.Dispose(_source);
                        return false;
                    }
                    return true;
                }

                private void NextHandler(Task<bool> t)
                {
                    if (t.IsFaulted)
                    {
                        _parent.InnerError(_index, ExceptionHelper.Extract(t.Exception));
                    }
                    else if (t.Result)
                    {
                        _parent.InnerNext(_index, _source.Current);
                    }
                    else
                    {
                        _parent.InnerComplete(_index);
                    }
                    if (TryDispose())
                    {
                        _parent.Signal();
                    }
                }

                internal void Dispose()
                {
                    if (Interlocked.Increment(ref _disposeWip) == 1)
                    {
                        _parent.Dispose(_source);
                    }
                }
            }
        }
    }
}
