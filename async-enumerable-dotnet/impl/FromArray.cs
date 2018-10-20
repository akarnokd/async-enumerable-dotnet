using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FromArray<T> : IAsyncEnumerable<T>
    {
        readonly T[] values;

        public FromArray(T[] values)
        {
            this.values = values;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromArrayEnumerator(values);
        }

        internal sealed class FromArrayEnumerator : IAsyncEnumerator<T>
        {
            readonly T[] values;

            int index;

            public T Current => values[index];

            public FromArrayEnumerator(T[] values)
            {
                this.values = values;
                this.index = -1;
            }

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                var idx = index + 1;
                if (idx < values.Length)
                {
                    index = idx;
                    return new ValueTask<bool>(true);
                }
                return new ValueTask<bool>(false);
            }
        }
    }
}
