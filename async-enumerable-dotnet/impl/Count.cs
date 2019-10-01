// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Count<TSource> : IAsyncEnumerable<long>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        public Count(IAsyncEnumerable<TSource> source)
        {
            _source = source;
        }

        public IAsyncEnumerator<long> GetAsyncEnumerator()
        {
            return new CountEnumerator(_source.GetAsyncEnumerator());
        }

        private sealed class CountEnumerator : IAsyncEnumerator<long>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            public long Current { get; private set; }

            private bool _once;
            
            public CountEnumerator(IAsyncEnumerator<TSource> source)
            {
                _source = source;
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    return false;
                }
                
                var n = 0L;

                while (await _source.MoveNextAsync())
                {
                    n++;
                }

                Current = n;
                _once = true;
                return true;
            }
        }
    }
}