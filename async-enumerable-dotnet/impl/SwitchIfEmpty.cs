// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SwitchIfEmpty<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly IAsyncEnumerable<T> _other;

        public SwitchIfEmpty(IAsyncEnumerable<T> source, IAsyncEnumerable<T> other)
        {
            _source = source;
            _other = other;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new SwitchIfEmptyEnumerator(_source.GetAsyncEnumerator(cancellationToken), _other, cancellationToken);
        }

        private sealed class SwitchIfEmptyEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerable<T> _other;

            private readonly CancellationToken _ct;

            private IAsyncEnumerator<T> _source;

            private bool _once;

            private bool _hasItems;

            public T Current => _source.Current;

            public SwitchIfEmptyEnumerator(IAsyncEnumerator<T> source, IAsyncEnumerable<T> other, CancellationToken ct)
            {
                _source = source;
                _other = other;
                _ct = ct;
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    return await _source.MoveNextAsync();
                }

                if (await _source.MoveNextAsync())
                {
                    _hasItems = true;
                    return true;
                }

                if (_hasItems)
                {
                    return false;
                }

                await _source.DisposeAsync();

                _source = _other.GetAsyncEnumerator(_ct);
                _once = true;

                return await _source.MoveNextAsync();
            }
        }
    }
}
