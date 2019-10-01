// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class IsEmpty<TSource> : IAsyncEnumerable<bool>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        public IsEmpty(IAsyncEnumerable<TSource> source)
        {
            _source = source;
        }

        public IAsyncEnumerator<bool> GetAsyncEnumerator()
        {
            return new IsEmptyEnumerator(_source.GetAsyncEnumerator());
        }

        private sealed class IsEmptyEnumerator : IAsyncEnumerator<bool>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            public bool Current { get; private set; }

            private bool _once;

            public IsEmptyEnumerator(IAsyncEnumerator<TSource> source)
            {
                _source = source;
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    return false;
                }

                _once = true;

                if (await _source.MoveNextAsync())
                {
                    Current = false;
                    return true;
                }

                Current = true;
                return true;
            }
        }
    }
}