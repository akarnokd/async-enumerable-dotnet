using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Collect<T, C> : IAsyncEnumerable<C>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<C> collectionSupplier;

        readonly Action<C, T> collector;

        public Collect(IAsyncEnumerable<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            this.source = source;
            this.collectionSupplier = collectionSupplier;
            this.collector = collector;
        }

        public IAsyncEnumerator<C> GetAsyncEnumerator()
        {
            var initial = default(C);
            try
            {
                initial = collectionSupplier();
            }
            catch (Exception ex)
            {
                return new Error<C>.ErrorEnumerator(ex);
            }
            return new CollectEnumerator(source.GetAsyncEnumerator(), initial, collector);
        }

        internal sealed class CollectEnumerator : IAsyncEnumerator<C>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Action<C, T> collector;

            C collection;

            bool once;

            public C Current => collection;

            public CollectEnumerator(IAsyncEnumerator<T> source, C collection, Action<C, T> collector)
            {
                this.source = source;
                this.collection = collection;
                this.collector = collector;
            }

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    collection = default;
                    return false;
                }
                once = true;

                while (await source.MoveNextAsync())
                {
                    collector(collection, source.Current);
                }

                return true;
            }
        }
    }
}
