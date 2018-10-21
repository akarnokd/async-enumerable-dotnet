using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Never<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
    {
        internal static readonly Never<T> Instance = new Never<T>();

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
            return new ValueTask<bool>(new TaskCompletionSource<bool>().Task);
        }
    }
}
