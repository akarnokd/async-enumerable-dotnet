// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Timeout<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly TimeSpan _timeout;

        public Timeout(IAsyncEnumerable<T> source, TimeSpan timeout)
        {
            _source = source;
            _timeout = timeout;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var sourceCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return new TimeoutEnumerator(_source.GetAsyncEnumerator(sourceCTS.Token), _timeout, sourceCTS);
        }

        private sealed class TimeoutEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly TimeSpan _timeout;

            private readonly CancellationTokenSource _sourceCTS;

            private TaskCompletionSource<bool> _disposeTask;

            public T Current => _source.Current;

            private long _index;

            private CancellationTokenSource _token;

            private int _disposeWip;

            public TimeoutEnumerator(IAsyncEnumerator<T> source, TimeSpan timeout, CancellationTokenSource cts)
            {
                _source = source;
                _timeout = timeout;
                _sourceCTS = cts;
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref _disposeWip) == 1)
                {
                    return _source.DisposeAsync();
                }
                return ResumeHelper.Await(ref _disposeTask);
            }

            public ValueTask<bool> MoveNextAsync()
            {
                var idx = Volatile.Read(ref _index);

                var result = new TaskCompletionSource<bool>();

                _token = CancellationTokenSource.CreateLinkedTokenSource(_sourceCTS.Token);

                Interlocked.Increment(ref _disposeWip);

                Task.Delay(_timeout, _token.Token)
                    .ContinueWith(t => Timeout(idx, result), _token.Token);

                _source.MoveNextAsync()
                    .AsTask().ContinueWith(t => Next(idx, t, result));

                return new ValueTask<bool>(result.Task);
            }

            private void Timeout(long idx, TaskCompletionSource<bool> result)
            {
                if (Interlocked.CompareExchange(ref _index, long.MaxValue, idx) == idx)
                {
                    result.TrySetException(new TimeoutException());
                }
            }

            private void Next(long idx, Task<bool> task, TaskCompletionSource<bool> result)
            {
                if (Interlocked.Decrement(ref _disposeWip) != 0)
                {
                    DisposeTask();
                }
                if (Interlocked.CompareExchange(ref _index, idx + 1, idx) == idx)
                {
                    _token?.Cancel();
                    if (task.IsCanceled)
                    {
                        result.TrySetCanceled();
                    }
                    else if (task.IsFaulted)
                    {
                        result.TrySetException(ExceptionHelper.Extract(task.Exception));
                    }
                    else
                    {
                        result.TrySetResult(task.Result);
                    }
                }
            }

            private void DisposeTask()
            {
                ResumeHelper.Complete(ref _disposeTask, _source.DisposeAsync());
            }
        }
    }
}
