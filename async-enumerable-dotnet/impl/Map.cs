// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Map<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, TResult> _mapper;

        public Map(IAsyncEnumerable<TSource> source, Func<TSource, TResult> mapper)
        {
            _source = source;
            _mapper = mapper;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            return new MapEnumerator(_source.GetAsyncEnumerator(), _mapper);
        }

        private sealed class MapEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, TResult> _mapper;

            public MapEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, TResult> mapper)
            {
                _source = source;
                _mapper = mapper;
            }

            public TResult Current { get; private set; }

            public ValueTask DisposeAsync()
            {
                Current = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await _source.MoveNextAsync())
                {
                    Current = _mapper(_source.Current);
                    return true;
                }
                return false;
            }
        }
    }

    internal sealed class MapAsync<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, Task<TResult>> _mapper;

        public MapAsync(IAsyncEnumerable<TSource> source, Func<TSource, Task<TResult>> mapper)
        {
            _source = source;
            _mapper = mapper;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            return new MapAsyncEnumerator(_source.GetAsyncEnumerator(), _mapper);
        }

        private sealed class MapAsyncEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, Task<TResult>> _mapper;

            public MapAsyncEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, Task<TResult>> mapper)
            {
                _source = source;
                _mapper = mapper;
            }

            public TResult Current { get; private set; }

            public ValueTask DisposeAsync()
            {
                Current = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await _source.MoveNextAsync())
                {
                    Current = await _mapper(_source.Current);
                    return true;
                }
                return false;
            }
        }
    }
}
