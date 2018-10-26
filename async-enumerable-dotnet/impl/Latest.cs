using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Latest<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        public Latest(IAsyncEnumerable<T> source)
        {
            this.source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new LatestEnumerator(source.GetAsyncEnumerator());
            en.MoveNext();
            return en;
        }

        internal sealed class LatestEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            int disposeWip;

            TaskCompletionSource<bool> disposeTask;

            long resumeWip;

            TaskCompletionSource<bool> resumeTask;

            Exception error;
            volatile bool done;

            object latest;

            int consumerWip;

            public T Current { get; private set; }

            readonly Action<Task<bool>> mainHandler;

            static readonly object EmptyIndicator = new object();

            public LatestEnumerator(IAsyncEnumerator<T> source)
            {
                this.source = source;
                this.mainHandler = t => HandleMain(t);
                Volatile.Write(ref latest, EmptyIndicator);
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref disposeWip) == 1)
                {
                    return source.DisposeAsync();
                }
                return new ValueTask(ResumeHelper.Resume(ref disposeTask).Task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = done;
                    var v = Interlocked.Exchange(ref latest, EmptyIndicator);
                    if (d && v == EmptyIndicator)
                    {
                        if (error != null)
                        {
                            throw error;
                        }
                        return false;
                    }
                    else if (v != EmptyIndicator)
                    {
                        Current = (T)v;
                        return true;
                    }

                    if (Volatile.Read(ref resumeWip) == 0)
                    {
                        await ResumeHelper.Resume(ref resumeTask).Task;
                    }
                    ResumeHelper.Clear(ref resumeTask, ref resumeWip);
                }
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
                                .AsTask()
                                .ContinueWith(mainHandler);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref consumerWip) != 0);
                }
            }

            void HandleMain(Task<bool> t)
            {
                if (Interlocked.Decrement(ref disposeWip) != 0)
                {
                    ResumeHelper.ResumeWhen(source.DisposeAsync(), ref disposeTask);
                }
                else
                {
                    if (t.IsFaulted)
                    {
                        error = ExceptionHelper.Unaggregate(t.Exception);
                        done = true;
                    }
                    else if (t.Result)
                    {
                        Interlocked.Exchange(ref latest, source.Current);
                    }
                    else
                    {
                        done = true;
                    }
                    ResumeHelper.Signal(ref resumeWip, ref resumeTask);
                }
            }
        }
    }
}
