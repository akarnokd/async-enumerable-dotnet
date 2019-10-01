// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Timer : IAsyncEnumerable<long>
    {
        private readonly TimeSpan _delay;

        public Timer(TimeSpan delay)
        {
            _delay = delay;
        }

        public IAsyncEnumerator<long> GetAsyncEnumerator()
        {
            return new TimerEnumerator(_delay);
        }

        private sealed class TimerEnumerator : IAsyncEnumerator<long>
        {
            private readonly TimeSpan _delay;

            private bool _once;

            public TimerEnumerator(TimeSpan delay)
            {
                _delay = delay;
            }

            public long Current => 0;

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    return false;
                }
                _once = true;

                await Task.Delay(_delay).ConfigureAwait(false);
                return true;
            }
        }
    }

    internal sealed class TimerCancellable : IAsyncEnumerable<long>
    {
        private readonly TimeSpan _delay;

        private readonly CancellationToken _token;

        public TimerCancellable(TimeSpan delay, CancellationToken token)
        {
            _delay = delay;
            _token = token;
        }

        public IAsyncEnumerator<long> GetAsyncEnumerator()
        {
            return new TimerEnumerator(_delay, _token);
        }

        private sealed class TimerEnumerator : IAsyncEnumerator<long>
        {
            private readonly TimeSpan _delay;

            private readonly CancellationToken _token;

            private bool _once;

            public TimerEnumerator(TimeSpan delay, CancellationToken token)
            {
                _delay = delay;
                _token = token;
            }

            public long Current => 0;

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    return false;
                }
                _once = true;

                await Task.Delay(_delay, _token).ConfigureAwait(false);
                return true;
            }
        }
    }
}
