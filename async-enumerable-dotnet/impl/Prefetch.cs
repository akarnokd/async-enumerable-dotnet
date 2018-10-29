using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Prefetch<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly int prefetch;

        readonly int limit;

        public Prefetch(IAsyncEnumerable<T> source, int prefetch, int limit)
        {
            this.source = source;
            this.prefetch = prefetch;
            this.limit = limit;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new PrefetchEnumerator(source.GetAsyncEnumerator(), prefetch, limit);
            en.MoveNext();
            return en;
        }

        internal sealed class PrefetchEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly int prefetch;

            readonly int limit;

            public T Current { get; private set; }

            int consumerWip;

            readonly ConcurrentQueue<T> queue;
            volatile bool done;
            Exception error;

            int consumed;

            int outstanding;

            TaskCompletionSource<bool> resume;

            int disposeWip;

            TaskCompletionSource<bool> disposeTask;

            readonly Action<Task<bool>> sourceHandler;

            public PrefetchEnumerator(IAsyncEnumerator<T> source, int prefetch, int limit)
            {
                this.source = source;
                this.prefetch = prefetch;
                this.limit = limit;
                this.sourceHandler = t => SourceHandler(t);
                this.queue = new ConcurrentQueue<T>();
                Volatile.Write(ref outstanding, prefetch);
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref disposeWip) == 1)
                {
                    return source.DisposeAsync();
                }
                return ResumeHelper.Await(ref disposeTask);
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref consumerWip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref disposeWip) == 1)
                        {
                            source.MoveNextAsync()
                                .AsTask().ContinueWith(sourceHandler);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref consumerWip) != 0);
                }
            }

            void Signal()
            {
                ResumeHelper.Resume(ref resume);
            }

            void SourceHandler(Task<bool> t)
            {
                var next = false;
                if (t.IsFaulted)
                {
                    error = ExceptionHelper.Extract(t.Exception);
                    done = true;
                }
                else if (t.Result)
                {
                    queue.Enqueue(source.Current);
                    next = true;
                }
                else
                {
                    done = true;
                }
                // release the MoveNext, just in case
                if (Interlocked.Decrement(ref disposeWip) != 0)
                {
                    ResumeHelper.Complete(ref disposeTask, source.DisposeAsync());
                }
                else
                {
                    Signal();

                    if (next && Interlocked.Decrement(ref outstanding) != 0)
                    {
                        MoveNext();
                    }
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = done;
                    var success = queue.TryDequeue(out var v);

                    if (d && !success)
                    {
                        if (error != null)
                        {
                            throw error;
                        }
                        return false;
                    }
                    else if (success)
                    {
                        Current = v;

                        var c = consumed + 1;
                        if (c == limit)
                        {
                            consumed = 0;
                            if (Interlocked.Add(ref outstanding, c) == c)
                            {
                                MoveNext();
                            }
                        }
                        else
                        {
                            consumed = c;
                        }

                        return true;
                    }

                    await ResumeHelper.Await(ref resume);
                    ResumeHelper.Clear(ref resume);
                }
            }
        }
    }
}
