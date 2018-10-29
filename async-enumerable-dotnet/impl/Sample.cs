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

        readonly bool emitLast;

        public Sample(IAsyncEnumerable<T> source, TimeSpan period, bool emitLast)
        {
            this.source = source;
            this.period = period;
            this.emitLast = emitLast;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new SampleEnumerator(source.GetAsyncEnumerator(), period, emitLast);
            en.StartTimer();
            en.MoveNext();
            return en;
        }

        internal sealed class SampleEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly TimeSpan period;

            readonly CancellationTokenSource cts;

            readonly bool emitLast;

            int consumerWip;

            TaskCompletionSource<bool> resume;

            object timerLatest;

            object latest;
            volatile bool done;
            Exception error;

            int disposeWip;

            TaskCompletionSource<bool> disposeTask;

            public T Current { get; private set; }

            static readonly object EmptyIndicator = new object();

            public SampleEnumerator(IAsyncEnumerator<T> source, TimeSpan period, bool emitLast)
            {
                this.source = source;
                this.period = period;
                this.emitLast = emitLast;
                this.disposeTask = new TaskCompletionSource<bool>();
                this.cts = new CancellationTokenSource();
                Volatile.Write(ref latest, EmptyIndicator);
            }

            public ValueTask DisposeAsync()
            {
                cts.Cancel();
                if (Interlocked.Increment(ref disposeWip) == 1)
                {
                    Interlocked.Exchange(ref timerLatest, EmptyIndicator);
                    return source.DisposeAsync();
                }
                return ResumeHelper.Await(ref disposeTask);
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

                    await ResumeHelper.Await(ref resume);
                    ResumeHelper.Clear(ref resume);
                }
            }

            // FIXME timer drift
            internal void StartTimer()
            {
                Task.Delay(period, cts.Token)
                    .ContinueWith(t => HandleTimer(t), cts.Token);
            }

            void HandleTimer(Task timer)
            {
                // take the saved timerLatest and make it available to MoveNextAsync
                // via latest
                Interlocked.Exchange(ref latest, Interlocked.Exchange(ref timerLatest, EmptyIndicator));

                Signal();
                StartTimer();
            }

            void Signal()
            {
                ResumeHelper.Resume(ref resume);
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

            bool TryDispose()
            {
                if (Interlocked.Decrement(ref disposeWip) != 0)
                {
                    Interlocked.Exchange(ref timerLatest, EmptyIndicator);
                    ResumeHelper.Complete(ref disposeTask, source.DisposeAsync());
                    return false;
                }
                return true;
            }

            void Handler(Task<bool> t)
            {
                if (t.IsFaulted)
                {
                    cts.Cancel();
                    if (emitLast)
                    {
                        Interlocked.Exchange(ref latest, Interlocked.Exchange(ref timerLatest, EmptyIndicator));
                    }
                    error = ExceptionHelper.Extract(t.Exception);
                    done = true;
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
                else if (t.Result)
                {
                    Interlocked.Exchange(ref timerLatest, source.Current);
                    if (TryDispose())
                    {
                        // the value will be picked up by the timer
                        MoveNext();
                    }
                }
                else
                {
                    cts.Cancel();
                    if (emitLast)
                    {
                        Interlocked.Exchange(ref latest, Interlocked.Exchange(ref timerLatest, EmptyIndicator));
                    }
                    done = true;
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
            }
        }
    }
}
