using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class DoOnDispose<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Action handler;

        public DoOnDispose(IAsyncEnumerable<T> source, Action handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DoOnDisposeEnumerator(source.GetAsyncEnumerator(), handler);
        }

        internal sealed class DoOnDisposeEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Action handler;

            public DoOnDisposeEnumerator(IAsyncEnumerator<T> source, Action handler)
            {
                this.source = source;
                this.handler = handler;
            }

            public T Current => source.Current;

            public ValueTask DisposeAsync()
            {
                handler();
                return source.DisposeAsync();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return source.MoveNextAsync();
            }
        }
    }

    internal sealed class DoOnDisposeAsync<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<ValueTask> handler;

        public DoOnDisposeAsync(IAsyncEnumerable<T> source, Func<ValueTask> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DoOnDisposeEnumerator(source.GetAsyncEnumerator(), handler);
        }

        internal sealed class DoOnDisposeEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<ValueTask> handler;

            public DoOnDisposeEnumerator(IAsyncEnumerator<T> source, Func<ValueTask> handler)
            {
                this.source = source;
                this.handler = handler;
            }

            public T Current => source.Current;

            public async ValueTask DisposeAsync()
            {
                await handler().ConfigureAwait(false);
                await source.DisposeAsync().ConfigureAwait(false);
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return source.MoveNextAsync();
            }
        }
    }
}
