// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Filter<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<T, bool> _predicate;

        public Filter(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new FilterEnumerator(_source.GetAsyncEnumerator(cancellationToken), _predicate);
        }

        private sealed class FilterEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Func<T, bool> _predicate;

            public FilterEnumerator(IAsyncEnumerator<T> source, Func<T, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            public T Current => _source.Current;

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    if (await _source.MoveNextAsync())
                    {
                        if (_predicate(_source.Current))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }

    internal sealed class FilterAsync<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<T, Task<bool>> _predicate;

        public FilterAsync(IAsyncEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new FilterAsyncEnumerator(_source.GetAsyncEnumerator(cancellationToken), _predicate);
        }

        private sealed class FilterAsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Func<T, Task<bool>> _predicate;

            public FilterAsyncEnumerator(IAsyncEnumerator<T> source, Func<T, Task<bool>> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            public T Current => _source.Current;

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (await _source.MoveNextAsync())
                    {
                        if (await _predicate(_source.Current))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}
