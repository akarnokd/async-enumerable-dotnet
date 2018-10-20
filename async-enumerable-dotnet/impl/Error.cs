using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Error<T> : IAsyncEnumerable<T>
    {
        readonly Exception error;

        public Error(Exception error)
        {
            this.error = error;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            throw new NotImplementedException();
        }

        internal sealed class ErrorEnumerator : IAsyncEnumerator<T>
        {
            readonly Exception error;

            bool once;

            public ErrorEnumerator(Exception error)
            {
                this.error = error;
            }

            public T Current => throw error;

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    await Task.CompletedTask;
                    return false;
                }
                once = true;
                throw error;
            }
        }
    }
}
