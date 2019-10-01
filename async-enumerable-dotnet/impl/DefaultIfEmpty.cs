// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class DefaultIfEmpty<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly T _defaultItem;

        public DefaultIfEmpty(IAsyncEnumerable<T> source, T defaultItem)
        {
            _source = source;
            _defaultItem = defaultItem;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new DefaultIfEmptyEnumerator(_source.GetAsyncEnumerator(cancellationToken), _defaultItem);
        }

        private sealed class DefaultIfEmptyEnumerator : IAsyncEnumerator<T>
        {
            private IAsyncEnumerator<T> _source;

            private readonly T _defaultItem;

            private bool _hasItems;

            public T Current
            {
                get
                {
                    if (_source != null)
                    {
                        return _source.Current;
                    }
                    return _defaultItem;
                }
            }

            public DefaultIfEmptyEnumerator(IAsyncEnumerator<T> source, T defaultItem)
            {
                _source = source;
                _defaultItem = defaultItem;
            }

            public ValueTask DisposeAsync()
            {
                if (_source != null)
                {
                    return _source.DisposeAsync();
                }
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_source == null)
                {
                    return false;
                }
                if (await _source.MoveNextAsync())
                {
                    _hasItems = true;
                    return true;
                }

                if (!_hasItems)
                {
                    await _source.DisposeAsync();
                    _source = null;
                    _hasItems = true;
                    return true;
                }
                return false;
            }
        }
    }
}
