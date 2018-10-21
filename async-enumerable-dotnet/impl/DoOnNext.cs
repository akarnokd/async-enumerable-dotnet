using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class DoOnNext<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Action<T> handler;

        public DoOnNext(IAsyncEnumerable<T> source, Action<T> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DoOnNextEnumerator(source.GetAsyncEnumerator(), handler);
        }

        internal sealed class DoOnNextEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Action<T> handler;

            public DoOnNextEnumerator(IAsyncEnumerator<T> source, Action<T> handler)
            {
                this.source = source;
                this.handler = handler;
            }

            public T Current => source.Current;

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await source.MoveNextAsync())
                {
                    handler(source.Current);
                    return true;
                }
                return false;
            }
        }
    }

    internal sealed class DoOnNextAsync<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, Task> handler;

        public DoOnNextAsync(IAsyncEnumerable<T> source, Func<T, Task> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DoOnNextAsyncEnumerator(source.GetAsyncEnumerator(), handler);
        }

        internal sealed class DoOnNextAsyncEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, Task> handler;

            public DoOnNextAsyncEnumerator(IAsyncEnumerator<T> source, Func<T, Task> handler)
            {
                this.source = source;
                this.handler = handler;
            }

            public T Current => source.Current;

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await source.MoveNextAsync())
                {
                    await handler(source.Current);
                    return true;
                }
                return false;
            }
        }
    }
}
