// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Skip<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly long _n;

        public Skip(IAsyncEnumerable<T> source, long n)
        {
            _source = source;
            _n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new SkipEnumerator(_source.GetAsyncEnumerator(), _n);
        }

        private sealed class SkipEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private long _remaining;

            public T Current => _source.Current;

            public SkipEnumerator(IAsyncEnumerator<T> source, long remaining)
            {
                _source = source;
                _remaining = remaining;
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                var n = _remaining;
                if (n != 0)
                {
                    while (n != 0)
                    {
                        if (await _source.MoveNextAsync())
                        {
                            n--;
                        }
                        else
                        {
                            _remaining = 0;
                            return false;
                        }
                    }
                    _remaining = 0;
                }

                return await _source.MoveNextAsync();
            }
        }
    }
}
