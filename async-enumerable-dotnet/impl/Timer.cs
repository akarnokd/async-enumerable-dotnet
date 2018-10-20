using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Timer : IAsyncEnumerable<long>
    {
        readonly TimeSpan delay;

        public Timer(TimeSpan delay)
        {
            this.delay = delay;
        }

        public IAsyncEnumerator<long> GetAsyncEnumerator()
        {
            return new TimerEnumerator(delay);
        }

        internal sealed class TimerEnumerator : IAsyncEnumerator<long>
        {
            readonly TimeSpan delay;

            bool once;

            public TimerEnumerator(TimeSpan delay)
            {
                this.delay = delay;
            }

            public long Current => 0;

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    return false;
                }
                once = true;

                await Task.Delay(delay).ConfigureAwait(false);
                return true;
            }
        }
    }

    internal sealed class TimerCancellable : IAsyncEnumerable<long>
    {
        readonly TimeSpan delay;

        readonly CancellationToken token;

        public TimerCancellable(TimeSpan delay, CancellationToken token)
        {
            this.delay = delay;
            this.token = token;
        }

        public IAsyncEnumerator<long> GetAsyncEnumerator()
        {
            return new TimerEnumerator(delay, token);
        }

        internal sealed class TimerEnumerator : IAsyncEnumerator<long>
        {
            readonly TimeSpan delay;

            readonly CancellationToken token;

            bool once;

            public TimerEnumerator(TimeSpan delay, CancellationToken token)
            {
                this.delay = delay;
                this.token = token;
            }

            public long Current => 0;

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    return false;
                }
                once = true;

                await Task.Delay(delay, token).ConfigureAwait(false);
                return true;
            }
        }
    }
}
