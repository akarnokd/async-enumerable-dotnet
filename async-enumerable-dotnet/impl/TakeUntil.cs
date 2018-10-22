using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class TakeUntil<T, U> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly IAsyncEnumerable<U> other;

        public TakeUntil(IAsyncEnumerable<T> source, IAsyncEnumerable<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new TakeUntilEnumerator(source.GetAsyncEnumerator(), other.GetAsyncEnumerator());
            en.MoveNextOther();
            return en;
        }

        internal sealed class TakeUntilEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly IAsyncEnumerator<U> other;

            public T Current => source.Current;

            TaskCompletionSource<bool> currentTask;

            Exception otherError;
            static readonly TaskCompletionSource<bool> UntilTask = new TaskCompletionSource<bool>();

            int disposeMain;

            int disposeOther;

            int disposed;
            TaskCompletionSource<bool> disposeReady;

            Exception disposeException;

            public TakeUntilEnumerator(IAsyncEnumerator<T> source, IAsyncEnumerator<U> other)
            {
                this.source = source;
                this.other = other;
                this.disposeReady = new TaskCompletionSource<bool>();
            }

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref disposeMain) == 1)
                {
                    Dispose(source);
                }
                if (Interlocked.Increment(ref disposeOther) == 1)
                {
                    Dispose(other);
                }

                await disposeReady.Task;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var task = Volatile.Read(ref currentTask);
                    if (task == UntilTask)
                    {
                        if (otherError != null)
                        {
                            return new ValueTask<bool>(Task.FromException<bool>(otherError));
                        }
                        return new ValueTask<bool>(false);
                    }
                    TaskCompletionSource<bool> newTask = new TaskCompletionSource<bool>();
                    if (Interlocked.CompareExchange(ref currentTask, newTask, task) == task)
                    {
                        if (Interlocked.Increment(ref disposeMain) == 1)
                        {
                            source.MoveNextAsync().AsTask()
                                .ContinueWith(t => HandleMain(t, newTask));
                            return new ValueTask<bool>(newTask.Task);
                        }
                    }
                }
            }

            void HandleMain(Task<bool> t, TaskCompletionSource<bool> newTask)
            {
                if (Interlocked.Decrement(ref disposeMain) != 0)
                {
                    Dispose(source);
                }
                else if (t.IsFaulted)
                {
                    newTask.TrySetException(t.Exception);
                }
                else
                {
                    if (t.Result)
                    {
                        newTask.TrySetResult(true);
                    }
                    else
                    {
                        newTask.TrySetResult(false);
                    }
                }
            }

            internal void MoveNextOther()
            {
                Interlocked.Increment(ref disposeOther);
                other.MoveNextAsync().AsTask()
                    .ContinueWith(t => HandleOther(t));
            }

            void HandleOther(Task<bool> t)
            {
                if (Interlocked.Decrement(ref disposeOther) != 0)
                {
                    Dispose(other);
                }
                else if (t.IsFaulted) {
                    otherError = t.Exception;
                    var oldTask = Interlocked.Exchange(ref currentTask, UntilTask);
                    if (oldTask != UntilTask)
                    {
                        oldTask?.TrySetException(t.Exception);
                    }
                }
                else
                {
                    var oldTask = Interlocked.Exchange(ref currentTask, UntilTask);
                    if (oldTask != UntilTask)
                    {
                        if (Interlocked.Increment(ref disposeMain) == 1)
                        {
                            Dispose(source);
                        }
                        if (Interlocked.Increment(ref disposeOther) == 1)
                        {
                            Dispose(other);
                        }
                        oldTask?.TrySetResult(false);
                    }
                }
            }

            void Dispose(IAsyncDisposable en)
            {
                en.DisposeAsync().AsTask()
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            ExceptionHelper.AddException(ref disposeException, t.Exception);
                        }
                        if (Interlocked.Increment(ref disposed) == 2)
                        {
                            var ex = disposeException;
                            if (ex != null)
                            {
                                disposeReady.TrySetException(ex);
                            }
                            else
                            {
                                disposeReady.TrySetResult(false);
                            }
                        }
                    });
            }
        }
    }
}
