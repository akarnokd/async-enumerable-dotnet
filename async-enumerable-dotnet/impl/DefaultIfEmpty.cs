using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class DefaultIfEmpty<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly T defaultItem;

        public DefaultIfEmpty(IAsyncEnumerable<T> source, T defaultItem)
        {
            this.source = source;
            this.defaultItem = defaultItem;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DefaultIfEmptyEnumerator(source.GetAsyncEnumerator(), defaultItem);
        }

        internal sealed class DefaultIfEmptyEnumerator : IAsyncEnumerator<T>
        {
            IAsyncEnumerator<T> source;

            readonly T defaultItem;

            bool hasItems;

            public T Current
            {
                get
                {
                    if (source != null)
                    {
                        return source.Current;
                    }
                    return defaultItem;
                }
            }

            public DefaultIfEmptyEnumerator(IAsyncEnumerator<T> source, T defaultItem)
            {
                this.source = source;
                this.defaultItem = defaultItem;
            }

            public ValueTask DisposeAsync()
            {
                if (source != null)
                {
                    return source.DisposeAsync();
                }
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (source == null)
                {
                    return false;
                }
                if (await source.MoveNextAsync())
                {
                    hasItems = true;
                    return true;
                }
                else if (!hasItems)
                {
                    await source.DisposeAsync();
                    source = null;
                    hasItems = true;
                    return true;
                }
                return false;
            }
        }
    }
}
