using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FromEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly IEnumerable<T> source;

        public FromEnumerable(IEnumerable<T> source)
        {
            this.source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromEnumerableEnumerator(source.GetEnumerator());
        }

        internal sealed class FromEnumerableEnumerator : IAsyncEnumerator<T>
        {
            readonly IEnumerator<T> source;

            public T Current => source.Current;

            public FromEnumerableEnumerator(IEnumerator<T> source)
            {
                this.source = source;
            }

            public ValueTask DisposeAsync()
            {
                source.Dispose();
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                if (source.MoveNext())
                {
                    return new ValueTask<bool>(true);
                }
                return new ValueTask<bool>(false);
            }
        }
    }
}
