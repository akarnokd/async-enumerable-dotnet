// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class TakeWhile<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<T, bool> _predicate;

        public TakeWhile(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TakeWhileEnumerator(_source.GetAsyncEnumerator(), _predicate);
        }

        private sealed class TakeWhileEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Func<T, bool> _predicate;

            public T Current => _source.Current;

            public TakeWhileEnumerator(IAsyncEnumerator<T> source, Func<T, bool> predicate)
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
                if (await _source.MoveNextAsync())
                {
                    return _predicate(_source.Current);
                }
                return false;
            }
        }
    }
}
