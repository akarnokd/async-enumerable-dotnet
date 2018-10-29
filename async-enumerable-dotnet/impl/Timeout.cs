using System;
using System.Threading;
using System.Threading.Tasks;

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

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TimeoutEnumerator(_source.GetAsyncEnumerator(), _timeout);
        }

        private sealed class TimeoutEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly TimeSpan _timeout;

            private TaskCompletionSource<bool> _disposeTask;

            public T Current => _source.Current;

            private long _index;

            private CancellationTokenSource _token;

            private int _disposeWip;

            public TimeoutEnumerator(IAsyncEnumerator<T> source, TimeSpan timeout)
            {
                _source = source;
                _timeout = timeout;
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
                if (idx == long.MaxValue)
                {
                    return new ValueTask<bool>(false);
                }

                var result = new TaskCompletionSource<bool>();

                _token = new CancellationTokenSource();

                Interlocked.Increment(ref _disposeWip);

                Task.Delay(_timeout, _token.Token)
                    .ContinueWith(t => Timeout(idx, result), _token.Token);

                var task = _source.MoveNextAsync();

                if (task.IsCompleted || task.IsFaulted)
                {
                    Next(idx, task, result);
                }
                else
                {
                    task.AsTask().ContinueWith(t => Next(idx, t, result));
                }

                return new ValueTask<bool>(result.Task);
            }

            private void Timeout(long idx, TaskCompletionSource<bool> result)
            {
                if (Interlocked.CompareExchange(ref _index, long.MaxValue, idx) == idx)
                {
                    result.TrySetException(new TimeoutException());
                }
            }

            private void Next(long idx, ValueTask<bool> task, TaskCompletionSource<bool> result)
            {
                if (Interlocked.Decrement(ref _disposeWip) != 0)
                {
                    DisposeTask();
                }
                if (Interlocked.CompareExchange(ref _index, idx + 1, idx) == idx)
                {
                    _token?.Cancel();
                    if (task.IsFaulted)
                    {
                        result.TrySetException(task.AsTask().Exception);
                    }
                    else
                    {
                        result.TrySetResult(task.Result);
                    }
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
                    if (task.IsFaulted)
                    {
                        result.TrySetException(task.Exception);
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
