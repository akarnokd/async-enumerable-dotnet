// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Prefetch<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly int _prefetch;

        private readonly int _limit;

        public Prefetch(IAsyncEnumerable<T> source, int prefetch, int limit)
        {
            _source = source;
            _prefetch = prefetch;
            _limit = limit;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new PrefetchEnumerator(_source.GetAsyncEnumerator(), _prefetch, _limit);
            en.MoveNext();
            return en;
        }

        private sealed class PrefetchEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly int _limit;

            public T Current { get; private set; }

            private int _consumerWip;

            private readonly ConcurrentQueue<T> _queue;
            private volatile bool _done;
            private Exception _error;

            private int _consumed;

            private int _outstanding;

            private TaskCompletionSource<bool> _resume;

            private int _disposeWip;

            private TaskCompletionSource<bool> _disposeTask;

            private readonly Action<Task<bool>> _sourceHandler;

            public PrefetchEnumerator(IAsyncEnumerator<T> source, int prefetch, int limit)
            {
                _source = source;
                _limit = limit;
                _sourceHandler = SourceHandler;
                _queue = new ConcurrentQueue<T>();
                Volatile.Write(ref _outstanding, prefetch);
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref _disposeWip) == 1)
                {
                    return _source.DisposeAsync();
                }
                return ResumeHelper.Await(ref _disposeTask);
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref _consumerWip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref _disposeWip) == 1)
                        {
                            _source.MoveNextAsync()
                                .AsTask().ContinueWith(_sourceHandler);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref _consumerWip) != 0);
                }
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }

            private void SourceHandler(Task<bool> t)
            {
                var next = false;
                if (t.IsFaulted)
                {
                    _error = ExceptionHelper.Extract(t.Exception);
                    _done = true;
                }
                else if (t.Result)
                {
                    _queue.Enqueue(_source.Current);
                    next = true;
                }
                else
                {
                    _done = true;
                }
                // release the MoveNext, just in case
                if (Interlocked.Decrement(ref _disposeWip) != 0)
                {
                    ResumeHelper.Complete(ref _disposeTask, _source.DisposeAsync());
                }
                else
                {
                    Signal();

                    if (next && Interlocked.Decrement(ref _outstanding) != 0)
                    {
                        MoveNext();
                    }
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = _done;
                    var success = _queue.TryDequeue(out var v);

                    if (d && !success)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }

                    if (success)
                    {
                        Current = v;

                        var c = _consumed + 1;
                        if (c == _limit)
                        {
                            _consumed = 0;
                            if (Interlocked.Add(ref _outstanding, c) == c)
                            {
                                MoveNext();
                            }
                        }
                        else
                        {
                            _consumed = c;
                        }

                        return true;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }
        }
    }
}
