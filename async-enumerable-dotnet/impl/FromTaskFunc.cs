using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FromTaskFunc<T> : IAsyncEnumerable<T>
    {
        readonly Func<Task<T>> func;

        public FromTaskFunc(Func<Task<T>> func)
        {
            this.func = func;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromTaskFuncEnumerator(func);
        }

        internal sealed class FromTaskFuncEnumerator : IAsyncEnumerator<T>
        {
            readonly Func<Task<T>> func;

            public T Current => current;

            T current;

            bool once;

            public FromTaskFuncEnumerator(Func<Task<T>> func)
            {
                this.func = func;
            }

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    current = default;
                    return false;
                }

                once = true;

                current = await func();
                return true;
            }
        }
    }

    internal sealed class FromTask<T> : IAsyncEnumerable<T>
    {
        readonly Task<T> task;

        public FromTask(Task<T> task)
        {
            this.task = task;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromTaskEnumerator(task);
        }

        internal sealed class FromTaskEnumerator : IAsyncEnumerator<T>
        {
            readonly Task<T> task;

            public T Current => task.Result;

            bool once;

            public FromTaskEnumerator(Task<T> task)
            {
                this.task = task;
            }

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

                await task;
                return true;
            }
        }
    }
}
