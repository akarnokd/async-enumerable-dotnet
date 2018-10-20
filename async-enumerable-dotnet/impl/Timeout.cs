using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Timeout<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly TimeSpan timeout;

        public Timeout(IAsyncEnumerable<T> source, TimeSpan timeout)
        {
            this.source = source;
            this.timeout = timeout;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TimeoutEnumerator(source.GetAsyncEnumerator(), timeout);
        }

        internal sealed class TimeoutEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly TimeSpan timeout;

            readonly TaskCompletionSource<int> dispose;

            public T Current => source.Current;

            long index;

            CancellationTokenSource token;

            int wip;

            public TimeoutEnumerator(IAsyncEnumerator<T> source, TimeSpan timeout)
            {
                this.source = source;
                this.timeout = timeout;
                this.dispose = new TaskCompletionSource<int>();
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    return source.DisposeAsync();
                }
                return new ValueTask(dispose.Task);
            }

            public ValueTask<bool> MoveNextAsync()
            {
                var idx = Volatile.Read(ref index);
                if (idx == long.MaxValue)
                {
                    return new ValueTask<bool>(false);
                }

                var result = new TaskCompletionSource<bool>();

                token = new CancellationTokenSource();

                Interlocked.Increment(ref wip);

                Task.Delay(timeout, token.Token)
                    .ContinueWith(t => Timeout(idx, result));

                var task = source.MoveNextAsync();

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

            void Timeout(long idx, TaskCompletionSource<bool> result)
            {
                if (Interlocked.CompareExchange(ref index, long.MaxValue, idx) == idx)
                {
                    result.TrySetException(new TimeoutException());
                }
            }

            void Next(long idx, ValueTask<bool> vtask, TaskCompletionSource<bool> result)
            {
                if (Interlocked.Decrement(ref wip) != 0)
                {
                    DisposeTask();
                }
                if (Interlocked.CompareExchange(ref index, idx + 1, idx) == idx)
                {
                    token?.Cancel();
                    if (vtask.IsFaulted)
                    {
                        try
                        {
                            var v = vtask.Result;
                        }
                        catch (Exception ex)
                        {
                            result.TrySetException(ex);
                        }
                    }
                    else
                    {
                        result.TrySetResult(vtask.Result);
                    }
                }
            }

            void Next(long idx, Task<bool> task, TaskCompletionSource<bool> result)
            {
                if (Interlocked.Decrement(ref wip) != 0)
                {
                    DisposeTask();
                }
                if (Interlocked.CompareExchange(ref index, idx + 1, idx) == idx)
                {
                    token?.Cancel();
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

            void DisposeTask()
            {
                source.DisposeAsync()
                    .AsTask()
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            dispose.TrySetException(t.Exception);
                        } else
                        {
                            dispose.TrySetResult(0);
                        }
                    });
            }
        }
    }
}
