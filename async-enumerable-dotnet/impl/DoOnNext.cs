// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class DoOnNext<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Action<T> _handler;

        public DoOnNext(IAsyncEnumerable<T> source, Action<T> handler)
        {
            _source = source;
            _handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DoOnNextEnumerator(_source.GetAsyncEnumerator(), _handler);
        }

        private sealed class DoOnNextEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Action<T> _handler;

            public DoOnNextEnumerator(IAsyncEnumerator<T> source, Action<T> handler)
            {
                _source = source;
                _handler = handler;
            }

            public T Current => _source.Current;

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await _source.MoveNextAsync())
                {
                    _handler(_source.Current);
                    return true;
                }
                return false;
            }
        }
    }

    internal sealed class DoOnNextAsync<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<T, Task> _handler;

        public DoOnNextAsync(IAsyncEnumerable<T> source, Func<T, Task> handler)
        {
            _source = source;
            _handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DoOnNextAsyncEnumerator(_source.GetAsyncEnumerator(), _handler);
        }

        private sealed class DoOnNextAsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Func<T, Task> _handler;

            public DoOnNextAsyncEnumerator(IAsyncEnumerator<T> source, Func<T, Task> handler)
            {
                _source = source;
                _handler = handler;
            }

            public T Current => _source.Current;

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await _source.MoveNextAsync())
                {
                    await _handler(_source.Current);
                    return true;
                }
                return false;
            }
        }
    }
}
