// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class DoOnDispose<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Action _handler;

        public DoOnDispose(IAsyncEnumerable<T> source, Action handler)
        {
            _source = source;
            _handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DoOnDisposeEnumerator(_source.GetAsyncEnumerator(), _handler);
        }

        private sealed class DoOnDisposeEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Action _handler;

            public DoOnDisposeEnumerator(IAsyncEnumerator<T> source, Action handler)
            {
                _source = source;
                _handler = handler;
            }

            public T Current => _source.Current;

            public async ValueTask DisposeAsync()
            {
                var ex = default(Exception);
                try
                {
                    _handler();
                }
                catch (Exception e)
                {
                    ex = e;
                }

                try
                {
                    await _source.DisposeAsync();
                }
                catch (Exception ex2)
                {
                    ex = ex == null ? ex2 : new AggregateException(ex, ex2);
                }

                if (ex != null)
                {
                    throw ex;
                }
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return _source.MoveNextAsync();
            }
        }
    }

    internal sealed class DoOnDisposeAsync<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<ValueTask> _handler;

        public DoOnDisposeAsync(IAsyncEnumerable<T> source, Func<ValueTask> handler)
        {
            _source = source;
            _handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new DoOnDisposeEnumerator(_source.GetAsyncEnumerator(), _handler);
        }

        private sealed class DoOnDisposeEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Func<ValueTask> _handler;

            public DoOnDisposeEnumerator(IAsyncEnumerator<T> source, Func<ValueTask> handler)
            {
                _source = source;
                _handler = handler;
            }

            public T Current => _source.Current;

            public async ValueTask DisposeAsync()
            {
                var ex = default(Exception);
                try
                {
                    await _handler().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ex = e;
                }
                try
                {
                    await _source.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    ex = ex == null ? ex2 : new AggregateException(ex, ex2);
                }

                if (ex != null)
                {
                    throw ex;
                }
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return _source.MoveNextAsync();
            }
        }
    }
}
