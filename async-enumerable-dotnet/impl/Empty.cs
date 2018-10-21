using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Empty<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
    {
        internal static readonly Empty<T> Instance = new Empty<T>();

        public T Current => default;

        public ValueTask DisposeAsync()
        {
            return new ValueTask();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return this;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(false);
        }
    }
}
