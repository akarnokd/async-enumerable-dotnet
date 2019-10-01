// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Collect<TSource, TCollection> : IAsyncEnumerable<TCollection>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TCollection> _collectionSupplier;

        private readonly Action<TCollection, TSource> _collector;

        public Collect(IAsyncEnumerable<TSource> source, Func<TCollection> collectionSupplier, Action<TCollection, TSource> collector)
        {
            _source = source;
            _collectionSupplier = collectionSupplier;
            _collector = collector;
        }

        public IAsyncEnumerator<TCollection> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            TCollection initial;
            try
            {
                initial = _collectionSupplier();
            }
            catch (Exception ex)
            {
                return new Error<TCollection>.ErrorEnumerator(ex);
            }
            return new CollectEnumerator(_source.GetAsyncEnumerator(cancellationToken), initial, _collector);
        }

        private sealed class CollectEnumerator : IAsyncEnumerator<TCollection>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Action<TCollection, TSource> _collector;

            private bool _once;

            public TCollection Current { get; private set; }

            public CollectEnumerator(IAsyncEnumerator<TSource> source, TCollection collection, Action<TCollection, TSource> collector)
            {
                _source = source;
                Current = collection;
                _collector = collector;
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    Current = default;
                    return false;
                }
                _once = true;

                while (await _source.MoveNextAsync())
                {
                    _collector(Current, _source.Current);
                }

                return true;
            }
        }
    }
}
