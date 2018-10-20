using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class CreateEmitter<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEmitter<T>, Task> handler;

        public CreateEmitter(Func<IAsyncEmitter<T>, Task> handler)
        {
            this.handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new CreateEmitterEnumerator();
            en.SetTask(handler(en));
            return en;
        }

        internal sealed class CreateEmitterEnumerator : IAsyncEnumerator<T>, IAsyncEmitter<T>
        {
            Task task;

            volatile bool disposeRequested;

            volatile bool taskComplete;

            public bool DisposeAsyncRequested => disposeRequested;

            public T Current => current;

            TaskCompletionSource<bool> valueReady;

            TaskCompletionSource<bool> consumed;

            T current;

            internal void SetTask(Task task)
            {
                this.task = task;
                task.ContinueWith(t =>
                {
                    taskComplete = true;
                    ValueReady().TrySetResult(false);
                });
            }

            public ValueTask DisposeAsync()
            {
                disposeRequested = true;
                return new ValueTask(task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (taskComplete)
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }
                    return false;
                }
                Consumed().TrySetResult(true);

                return await ValueReady().Task;
            }

            public async ValueTask Next(T value)
            {
                await Consumed().Task;

                current = value;

                ValueReady().TrySetResult(true);
            }

            TaskCompletionSource<bool> ValueReady()
            {
                var b = default(TaskCompletionSource<bool>);
                for (;;)
                {
                    var a = Volatile.Read(ref valueReady);

                    if (a == null || a.Task.IsCompleted)
                    {
                        if (b == null)
                        {
                            b = new TaskCompletionSource<bool>();
                        }
                    } else
                    {
                        return a;
                    }

                    if (Interlocked.CompareExchange(ref valueReady, b, a) == a)
                    {
                        return b;
                    }
                }
            }

            TaskCompletionSource<bool> Consumed()
            {
                var b = default(TaskCompletionSource<bool>);
                for (; ; )
                {
                    var a = Volatile.Read(ref consumed);

                    if (a == null || a.Task.IsCompleted)
                    {
                        if (b == null)
                        {
                            b = new TaskCompletionSource<bool>();
                        }
                    }
                    else
                    {
                        return a;
                    }

                    if (Interlocked.CompareExchange(ref consumed, b, a) == a)
                    {
                        return b;
                    }
                }
            }
        }
    }
}
