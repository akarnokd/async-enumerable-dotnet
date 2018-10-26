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

            long wip;

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
                return new ValueTask(ResumeHelper.Resume(ref disposeTask).Task);
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
                if (Interlocked.Increment(ref wip) == 1)
                {
                    ResumeHelper.Resume(ref resume).TrySetResult(true);
                }
            }

            void SourceHandler(Task<bool> t)
            {
                if (Interlocked.Decrement(ref disposeWip) != 0)
                {
                    ResumeHelper.ResumeWhen(source.DisposeAsync(), ref disposeTask);
                }
                else if (t.IsFaulted)
                {
                    error = ExceptionHelper.Unaggregate(t.Exception);
                    done = true;
                    Signal();
                }
                else if (t.Result)
                {
                    queue.Enqueue(source.Current);
                    Signal();
                    if (Interlocked.Decrement(ref outstanding) != 0)
                    {
                        MoveNext();
                    }
                }
                else
                {
                    done = true;
                    Signal();
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

                    if (Volatile.Read(ref wip) == 0)
                    {
                        await ResumeHelper.Resume(ref resume).Task;
                    }
                    ResumeHelper.Clear(ref resume);
                    Interlocked.Exchange(ref wip, 0);
                }
            }
        }
    }
}
