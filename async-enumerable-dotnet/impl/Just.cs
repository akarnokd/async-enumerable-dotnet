using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Just<T> : IAsyncEnumerable<T>
    {
        readonly T value;

        public Just(T value)
        {
            this.value = value;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new JustEnumerator(value);
        }

        internal sealed class JustEnumerator : IAsyncEnumerator<T>
        {
            readonly T value;

            bool once;

            public JustEnumerator(T value)
            {
                this.value = value;
            }

            public T Current => value;

            public ValueTask DisposeAsync()
            {
                // deliberately no-op
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    return new ValueTask<bool>(false);
                }
                once = true;
                return new ValueTask<bool>(true);
            }
        }
    }
}
