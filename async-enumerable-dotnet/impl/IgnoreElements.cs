using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class IgnoreElements<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        public IgnoreElements(IAsyncEnumerable<T> source)
        {
            this.source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new IgnoreElementsEnumerator(source.GetAsyncEnumerator());
        }

        internal sealed class IgnoreElementsEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            public IgnoreElementsEnumerator(IAsyncEnumerator<T> source)
            {
                this.source = source;
            }

            public T Current => default;

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                while (await source.MoveNextAsync()) ;

                return false;
            }
        }
    }
}
