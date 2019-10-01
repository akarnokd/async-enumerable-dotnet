// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FromTaskFunc<T> : IAsyncEnumerable<T>
    {
        private readonly Func<Task<T>> _func;

        public FromTaskFunc(Func<Task<T>> func)
        {
            _func = func;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromTaskFuncEnumerator(_func);
        }

        private sealed class FromTaskFuncEnumerator : IAsyncEnumerator<T>
        {
            private readonly Func<Task<T>> _func;

            public T Current { get; private set; }

            private bool _once;

            public FromTaskFuncEnumerator(Func<Task<T>> func)
            {
                _func = func;
            }

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    Current = default;
                    return false;
                }

                _once = true;

                Current = await _func();
                return true;
            }
        }
    }

    internal sealed class FromTask<T> : IAsyncEnumerable<T>
    {
        private readonly Task<T> _task;

        public FromTask(Task<T> task)
        {
            _task = task;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromTaskEnumerator(_task);
        }

        private sealed class FromTaskEnumerator : IAsyncEnumerator<T>
        {
            private readonly Task<T> _task;

            public T Current => _task.Result;

            private bool _once;

            public FromTaskEnumerator(Task<T> task)
            {
                _task = task;
            }

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

                await _task;
                return true;
            }
        }
    }
}
