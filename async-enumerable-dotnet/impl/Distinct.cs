// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Distinct<TSource, TKey> : IAsyncEnumerable<TSource>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, TKey> _keySelector;

        private readonly Func<ISet<TKey>> _collectionSupplier;

        public Distinct(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<ISet<TKey>> collectionSupplier)
        {
            _source = source;
            _keySelector = keySelector;
            _collectionSupplier = collectionSupplier;
        }

        public IAsyncEnumerator<TSource> GetAsyncEnumerator()
        {
            ISet<TKey> collection;

            try
            {
                collection = _collectionSupplier();
            }
            catch (Exception ex)
            {
                return new Error<TSource>.ErrorEnumerator(ex);
            }
            return new DistinctEnumerator(_source.GetAsyncEnumerator(), _keySelector, collection);
        }

        private sealed class DistinctEnumerator : IAsyncEnumerator<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, TKey> _keySelector;

            private ISet<TKey> _collection;

            public TSource Current => _source.Current;

            public DistinctEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, TKey> keySelector, ISet<TKey> collection)
            {
                _source = source;
                _keySelector = keySelector;
                _collection = collection;
            }

            public ValueTask DisposeAsync()
            {
                _collection = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (await _source.MoveNextAsync())
                    {
                        if (_collection.Add(_keySelector(_source.Current)))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}
