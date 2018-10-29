// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Error<T> : IAsyncEnumerable<T>
    {
        private readonly Exception _error;

        public Error(Exception error)
        {
            _error = error;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ErrorEnumerator(_error);
        }

        internal sealed class ErrorEnumerator : IAsyncEnumerator<T>
        {
            private readonly Exception _error;

            private bool _once;

            public ErrorEnumerator(Exception error)
            {
                _error = error;
            }

            public T Current => throw _error;

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    await Task.CompletedTask;
                    return false;
                }
                _once = true;
                throw _error;
            }
        }
    }
}
