using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SwitchIfEmpty<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly IAsyncEnumerable<T> other;

        public SwitchIfEmpty(IAsyncEnumerable<T> source, IAsyncEnumerable<T> other)
        {
            this.source = source;
            this.other = other;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new SwitchIfEmptyEnumerator(source.GetAsyncEnumerator(), other);
        }

        internal sealed class SwitchIfEmptyEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerable<T> other;

            IAsyncEnumerator<T> source;

            bool once;

            bool hasItems;

            public T Current => source.Current;

            public SwitchIfEmptyEnumerator(IAsyncEnumerator<T> source, IAsyncEnumerable<T> other)
            {
                this.source = source;
                this.other = other;
            }

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    return await source.MoveNextAsync();
                }
                else
                {
                    if (await source.MoveNextAsync())
                    {
                        hasItems = true;
                        return true;
                    }

                    if (hasItems)
                    {
                        return false;
                    }

                    await source.DisposeAsync();

                    source = other.GetAsyncEnumerator();
                    once = true;

                    return await source.MoveNextAsync();
                }
            }
        }
    }
}
