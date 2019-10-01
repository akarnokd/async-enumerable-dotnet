// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Take<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly long _n;

        public Take(IAsyncEnumerable<T> source, long n)
        {
            _source = source;
            _n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new TakeEnumerator(_source.GetAsyncEnumerator(cancellationToken), _n);
        }

        private sealed class TakeEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private long _remaining;

            private bool _once;

            public TakeEnumerator(IAsyncEnumerator<T> source, long remaining)
            {
                _source = source;
                _remaining = remaining;
            }

            public T Current => _source.Current;

            public ValueTask DisposeAsync()
            {
                if (!_once)
                {
                    _once = true;
                    return _source.DisposeAsync();
                }
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                var n = _remaining;
                if (n == 0)
                {
                    // eagerly dispose as who knows when the
                    // consumer will call DisposeAsync?
                    await DisposeAsync();
                    return false;
                }
                _remaining = n - 1;

                return await _source.MoveNextAsync();
            }
        }
    }
}
