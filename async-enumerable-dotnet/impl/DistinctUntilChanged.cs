// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class DistinctUntilChanged<TSource, TKey> : IAsyncEnumerable<TSource>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, TKey> _keySelector;

        private readonly IEqualityComparer<TKey> _keyComparer;

        public DistinctUntilChanged(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
        {
            _source = source;
            _keySelector = keySelector;
            _keyComparer = keyComparer;
        }

        public IAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new DistinctUntilChangedEnumerator(_source.GetAsyncEnumerator(cancellationToken), _keySelector, _keyComparer);
        }

        private sealed class DistinctUntilChangedEnumerator : IAsyncEnumerator<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, TKey> _keySelector;

            private readonly IEqualityComparer<TKey> _keyComparer;

            private TKey _prevKey;

            public TSource Current => _source.Current;

            private bool _once;

            public DistinctUntilChangedEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
            {
                _source = source;
                _keySelector = keySelector;
                _keyComparer = keyComparer;
            }

            public ValueTask DisposeAsync()
            {
                _prevKey = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    if (await _source.MoveNextAsync())
                    {
                        var key = _keySelector(_source.Current);
                        if (!_once)
                        {
                            _once = true;
                            _prevKey = key;
                            return true;
                        }
                        if (!_keyComparer.Equals(_prevKey, key))
                        {
                            _prevKey = key;
                            return true;
                        }

                        _prevKey = key;
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
