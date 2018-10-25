using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Sample<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly TimeSpan period;

        public Sample(IAsyncEnumerable<T> source, TimeSpan period)
        {
            this.source = source;
            this.period = period;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new SampleEnumerator(source.GetAsyncEnumerator(), period);
            en.StartTimer();
            en.MoveNext();
            return en;
        }

        internal sealed class SampleEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly TimeSpan period;

            readonly CancellationTokenSource cts;

            int consumerWip;

            TaskCompletionSource<bool> resume;

            object latest;
            volatile bool done;
            Exception error;

            long wip;

            int disposeWip;

            readonly TaskCompletionSource<bool> disposeTask;

            public T Current { get; private set; }

            static readonly object EmptyIndicator = new object();

            public SampleEnumerator(IAsyncEnumerator<T> source, TimeSpan period)
            {
                this.source = source;
                this.period = period;
                this.disposeTask = new TaskCompletionSource<bool>();
                this.cts = new CancellationTokenSource();
                Volatile.Write(ref latest, EmptyIndicator);
            }

            public ValueTask DisposeAsync()
            {
                cts.Cancel();
                if (Interlocked.Increment(ref disposeWip) == 1)
                {
                    return source.DisposeAsync();
                }
                return new ValueTask(disposeTask.Task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    bool d = done;
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

                    if (Volatile.Read(ref wip) == 0)
                    {
                        await ResumeHelper.Resume(ref resume).Task;
                    }
                    ResumeHelper.Clear(ref resume);
                    Interlocked.Exchange(ref wip, 0);
                }
            }

            internal void StartTimer()
            {
                Task.Delay(period, cts.Token)
                    .ContinueWith(t => HandleTimer(t), cts.Token);
            }

            void HandleTimer(Task timer)
            {
                Signal();
                StartTimer();
            }

            void Signal()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    ResumeHelper.Resume(ref resume).TrySetResult(true);
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
                                .AsTask().ContinueWith(t => Handler(t));
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref consumerWip) != 0);
                }
            }

            void Handler(Task<bool> t)
            {
                if (Interlocked.Decrement(ref disposeWip) != 0)
                {
                    ResumeHelper.ResumeWhen(source.DisposeAsync(), disposeTask);
                }
                else if (t.IsFaulted)
                {
                    error = ExceptionHelper.Unaggregate(t.Exception);
                    done = true;
                    cts.Cancel();
                    Signal();
                }
                else if (t.Result)
                {
                    Interlocked.Exchange(ref latest, source.Current);
                    // resumption will be triggered by the timer
                    MoveNext();
                }
                else
                {
                    done = true;
                    cts.Cancel();
                    Signal();
                }
            }
        }
    }
}
