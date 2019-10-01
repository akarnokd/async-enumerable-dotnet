// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

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

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new RetryEnumerator(_source, _source.GetAsyncEnumerator(cancellationToken), _n, _condition, cancellationToken);
        }

        private sealed class RetryEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerable<T> _source;

            private readonly CancellationToken _ct;

            private IAsyncEnumerator<T> _current;

            private long _remaining;

            private readonly Func<long, Exception, bool> _condition;

            public T Current => _current.Current;

            private long _index;

            public RetryEnumerator(IAsyncEnumerable<T> source, IAsyncEnumerator<T> current, long remaining, 
                Func<long, Exception, bool> condition, CancellationToken ct)
            {
                _source = source;
                _current = current;
                _remaining = remaining;
                _condition = condition;
                _ct = ct;
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

                            _current = _source.GetAsyncEnumerator(_ct);
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

    internal sealed class RetryTask<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<long, Exception, Task<bool>> _condition;

        public RetryTask(IAsyncEnumerable<T> source, Func<long, Exception, Task<bool>> condition)
        {
            _source = source;
            _condition = condition;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new RetryTaskEnumerator(_source, _source.GetAsyncEnumerator(cancellationToken), _condition, cancellationToken);
        }

        private sealed class RetryTaskEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerable<T> _source;

            private readonly CancellationToken _ct;

            private IAsyncEnumerator<T> _current;

            private readonly Func<long, Exception, Task<bool>> _condition;

            public T Current => _current.Current;

            private long _index;

            public RetryTaskEnumerator(IAsyncEnumerable<T> source, IAsyncEnumerator<T> current, 
                Func<long, Exception, Task<bool>> condition, CancellationToken ct)
            {
                _source = source;
                _current = current;
                _condition = condition;
                _ct = ct;
            }

            public ValueTask DisposeAsync()
            {
                return _current.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    try
                    {
                        return await _current.MoveNextAsync();
                    }
                    catch (Exception ex)
                    {
                        if (await _condition(_index++, ex))
                        {
                            await _current.DisposeAsync();

                            _current = _source.GetAsyncEnumerator(_ct);
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
