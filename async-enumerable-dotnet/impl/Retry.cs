// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Retry<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly long _n;

        private readonly Func<long, Exception, bool> _condition;

        public Retry(IAsyncEnumerable<T> source, long n, Func<long, Exception, bool> condition)
        {
            _source = source;
            _n = n;
            _condition = condition;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new RetryEnumerator(_source, _source.GetAsyncEnumerator(), _n, _condition);
        }

        private sealed class RetryEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerable<T> _source;

            private IAsyncEnumerator<T> _current;

            private long _remaining;

            private readonly Func<long, Exception, bool> _condition;

            public T Current => _current.Current;

            private long _index;

            public RetryEnumerator(IAsyncEnumerable<T> source, IAsyncEnumerator<T> current, long remaining, Func<long, Exception, bool> condition)
            {
                _source = source;
                _current = current;
                _remaining = remaining;
                _condition = condition;
            }

            public ValueTask DisposeAsync()
            {
                return _current.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    try
                    {
                        return await _current.MoveNextAsync();
                    }
                    catch (Exception ex)
                    {
                        var n = _remaining - 1;
                        if (n <= 0)
                        {
                            throw;
                        }

                        if (_condition(_index++, ex))
                        {
                            _remaining = n;

                            await _current.DisposeAsync();

                            _current = _source.GetAsyncEnumerator();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }
    }
}
