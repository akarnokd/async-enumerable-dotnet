// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Repeat<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly long _n;

        private readonly Func<long, bool> _condition;

        public Repeat(IAsyncEnumerable<T> source, long n, Func<long, bool> condition)
        {
            _source = source;
            _n = n;
            _condition = condition;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new RepeatEnumerator(_source, _condition, _source.GetAsyncEnumerator(cancellationToken), _n, cancellationToken);
        }

        private sealed class RepeatEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerable<T> _source;

            private readonly Func<long, bool> _condition;

            private readonly CancellationToken _ct;

            private IAsyncEnumerator<T> _current;

            private long _remaining;

            private long _index;

            public T Current => _current.Current;

            public RepeatEnumerator(IAsyncEnumerable<T> source, Func<long, bool> condition, IAsyncEnumerator<T> current, 
                long remaining, CancellationToken ct)
            {
                _source = source;
                _condition = condition;
                _current = current;
                _remaining = remaining;
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
                    if (await _current.MoveNextAsync())
                    {
                        return true;
                    }

                    var n = _remaining - 1;

                    if (n <= 0)
                    {
                        return false;
                    }

                    if (_condition(_index++))
                    {
                        await _current.DisposeAsync();

                        _current = _source.GetAsyncEnumerator(_ct);

                        _remaining = n;
                    }
                    else
                    {
                        return false;
                    }

                }
            }
        }
    }

    internal sealed class RepeatTask<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<long, Task<bool>> _condition;

        public RepeatTask(IAsyncEnumerable<T> source, Func<long, Task<bool>> condition)
        {
            _source = source;
            _condition = condition;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new RepeatTaskEnumerator(_source, _condition, _source.GetAsyncEnumerator(cancellationToken), cancellationToken);
        }

        private sealed class RepeatTaskEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerable<T> _source;

            private readonly Func<long, Task<bool>> _condition;

            private readonly CancellationToken _ct;

            private IAsyncEnumerator<T> _current;

            private long _index;

            public T Current => _current.Current;

            public RepeatTaskEnumerator(IAsyncEnumerable<T> source, Func<long, Task<bool>> condition, IAsyncEnumerator<T> current,
                CancellationToken ct)
            {
                _source = source;
                _condition = condition;
                _current = current;
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
                    if (await _current.MoveNextAsync())
                    {
                        return true;
                    }

                    if (await _condition(_index++))
                    {
                        await _current.DisposeAsync();

                        _current = _source.GetAsyncEnumerator(_ct);
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
