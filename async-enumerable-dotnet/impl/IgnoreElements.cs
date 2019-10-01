// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class IgnoreElements<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        public IgnoreElements(IAsyncEnumerable<T> source)
        {
            _source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new IgnoreElementsEnumerator(_source.GetAsyncEnumerator());
        }

        private sealed class IgnoreElementsEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            public IgnoreElementsEnumerator(IAsyncEnumerator<T> source)
            {
                _source = source;
            }

            public T Current => default;

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                while (await _source.MoveNextAsync())
                {
                    // deliberately ignoring items
                }

                return false;
            }
        }
    }
}
