// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Interval : IAsyncEnumerable<long>
    {
        private readonly long _start;

        private readonly long _end;

        private readonly TimeSpan _initialDelay;

        private readonly TimeSpan _period;

        public Interval(long start, long end, TimeSpan initialDelay, TimeSpan period)
        {
            _start = start;
            _end = end;
            _initialDelay = initialDelay;
            _period = period;
        }

        public IAsyncEnumerator<long> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var en = new IntervalEnumerator(_period, _start, _end, cancellationToken);
            en.StartFirst(_initialDelay);
            return en;
        }

        private sealed class IntervalEnumerator : IAsyncEnumerator<long>
        {
            private readonly long _end;

            private readonly TimeSpan _period;

            private readonly CancellationTokenSource _cts;

            private long _available;

            private long _index;

            private TaskCompletionSource<bool> _resume;

            public long Current { get; private set; }

            public IntervalEnumerator(TimeSpan period, long start, long end, CancellationToken ct)
            {
                _period = period;
                _end = end;
                _available = start;
                _index = start;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                Volatile.Write(ref _available, start);
            }

            public ValueTask DisposeAsync()
            {
                _cts.Cancel();
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var a = Volatile.Read(ref _available);
                    var b = _index;

                    if (a != b)
                    {
                        Current = b;
                        _index = b + 1;
                        return true;
                    }
                    if (b == _end)
                    {
                        return false;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            internal void StartFirst(TimeSpan initialDelay)
            {
                Task.Delay(initialDelay, _cts.Token)
                    .ContinueWith(NextAction, this);
            }

            private static readonly Action<Task, object> NextAction = (t, state) =>
                ((IntervalEnumerator) state).Next(t);

            private void Next(Task t)
            {
                if (t.IsCanceled || _cts.IsCancellationRequested)
                {
                    ResumeHelper.Resume(ref _resume);
                    return;
                }
                var value = Interlocked.Increment(ref _available);
                ResumeHelper.Resume(ref _resume);

                if (value != _end)
                {
                    // FIXME compensate for drifts
                    Task.Delay(_period, _cts.Token)
                        .ContinueWith(NextAction, this);
                }
            }

        }
    }
}
