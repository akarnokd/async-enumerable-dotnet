using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Interval : IAsyncEnumerable<long>
    {
        readonly long start;

        readonly long end;

        readonly TimeSpan initialDelay;

        readonly TimeSpan period;

        public Interval(long start, long end, TimeSpan initialDelay, TimeSpan period)
        {
            this.start = start;
            this.end = end;
            this.initialDelay = initialDelay;
            this.period = period;
        }

        public IAsyncEnumerator<long> GetAsyncEnumerator()
        {
            var en = new IntervalEnumerator(period, start, end);
            en.StartFirst(initialDelay);
            return en;
        }

        internal sealed class IntervalEnumerator : IAsyncEnumerator<long>
        {
            readonly long end;

            readonly TimeSpan period;

            readonly CancellationTokenSource cts;

            long available;

            long index;

            long current;

            long wip;

            TaskCompletionSource<bool> resume;

            public long Current => current;

            public IntervalEnumerator(TimeSpan period, long start, long end)
            {
                this.period = period;
                this.end = end;
                this.available = start;
                this.index = start;
                this.cts = new CancellationTokenSource();
                Volatile.Write(ref available, start);
            }

            public ValueTask DisposeAsync()
            {
                cts.Cancel();
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    var a = Volatile.Read(ref available);
                    var b = index;

                    if (a != b)
                    {
                        current = b;
                        index = b + 1;
                        return true;
                    }
                    if (b == end)
                    {
                        return false;
                    }

                    if (Volatile.Read(ref wip) == 0)
                    {
                        await ResumeHelper.Resume(ref resume).Task;
                    }
                    ResumeHelper.Clear(ref resume);
                    Interlocked.Exchange(ref wip, 0);
                }
            }

            internal void StartFirst(TimeSpan initialDelay)
            {
                Task.Delay(initialDelay, cts.Token)
                    .ContinueWith(t => Next(t));
            }

            void Next(Task t)
            {
                if (t.IsCanceled || cts.IsCancellationRequested)
                {
                    return;
                }
                var value = Interlocked.Increment(ref available);
                if (Interlocked.Increment(ref wip) == 1)
                {
                    ResumeHelper.Resume(ref resume).TrySetResult(true);
                }

                if (value != end)
                {
                    // FIXME compensate for drifts
                    Task.Delay(period, cts.Token)
                        .ContinueWith(x => Next(x));
                }
            }

        }
    }
}
