// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SkipWhile<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<T, bool> _predicate;

        public SkipWhile(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new SkipWhileEnumerator(_source.GetAsyncEnumerator(cancellationToken), _predicate);
        }

        private sealed class SkipWhileEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Func<T, bool> _predicate;

            public T Current => _source.Current;

            private bool _once;

            public SkipWhileEnumerator(IAsyncEnumerator<T> source, Func<T, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
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

                for (; ;)
                {
                    if (await _source.MoveNextAsync())
                    {
                        if (!_predicate(_source.Current))
                        {
                            _once = true;
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
