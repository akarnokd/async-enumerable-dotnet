using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Take<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly long n;

        public Take(IAsyncEnumerable<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TakeEnumerator(source.GetAsyncEnumerator(), n);
        }

        internal sealed class TakeEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            long remaining;

            bool once;

            public TakeEnumerator(IAsyncEnumerator<T> source, long remaining)
            {
                this.source = source;
                this.remaining = remaining;
            }

            public T Current => source.Current;

            public ValueTask DisposeAsync()
            {
                if (!once)
                {
                    once = true;
                    return source.DisposeAsync();
                }
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                long n = remaining;
                if (n == 0)
                {
                    // eagerly dispose as who knows when the
                    // consumer will call DisposeAsync?
                    await DisposeAsync();
                    return false;
                }
                remaining = n - 1;

                return await source.MoveNextAsync();
            }
        }
    }
}
