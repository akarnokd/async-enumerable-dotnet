using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Skip<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly long n;

        public Skip(IAsyncEnumerable<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new SkipEnumerator(source.GetAsyncEnumerator(), n);
        }

        internal sealed class SkipEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            long remaining;

            public T Current => source.Current;

            public SkipEnumerator(IAsyncEnumerator<T> source, long remaining)
            {
                this.source = source;
                this.remaining = remaining;
            }

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                var n = remaining;
                if (n != 0)
                {
                    while (n != 0)
                    {
                        if (await source.MoveNextAsync())
                        {
                            n--;
                        }
                        else
                        {
                            remaining = 0;
                            return false;
                        }
                    }
                    remaining = 0;
                }

                return await source.MoveNextAsync();
            }
        }
    }
}
