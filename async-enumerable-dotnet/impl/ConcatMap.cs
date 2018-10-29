// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ConcatMap<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, IAsyncEnumerable<TResult>> _mapper;

        public ConcatMap(IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper)
        {
            _source = source;
            _mapper = mapper;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            return new ConcatMapEnumerator(_source.GetAsyncEnumerator(), _mapper);
        }

        private sealed class ConcatMapEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, IAsyncEnumerable<TResult>> _mapper;

            public TResult Current { get; private set; }

            private IAsyncEnumerator<TResult> _inner;

            public ConcatMapEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper)
            {
                _source = source;
                _mapper = mapper;
            }

            public async ValueTask DisposeAsync()
            {
                if (_inner != null)
                {
                    await _inner.DisposeAsync();
                }
                await _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                Current = default;
                for (; ;)
                {
                    if (_inner == null)
                    {
                        if (await _source.MoveNextAsync())
                        {
                            _inner = _mapper(_source.Current).GetAsyncEnumerator();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (await _inner.MoveNextAsync())
                    {
                        Current = _inner.Current;
                        return true;
                    }

                    await _inner.DisposeAsync();
                    _inner = null;
                }
            }
        }
    }

    internal sealed class ConcatMapEnumerable<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, IEnumerable<TResult>> _mapper;

        public ConcatMapEnumerable(IAsyncEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> mapper)
        {
            _source = source;
            _mapper = mapper;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            return new ConcatMapEnumerator(_source.GetAsyncEnumerator(), _mapper);
        }

        private sealed class ConcatMapEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, IEnumerable<TResult>> _mapper;

            public TResult Current { get; private set; }

            private IEnumerator<TResult> _inner;

            public ConcatMapEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, IEnumerable<TResult>> mapper)
            {
                _source = source;
                _mapper = mapper;
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                    _inner?.Dispose();
                }
                finally
                {
                    await _source.DisposeAsync();
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                Current = default;
                for (; ; )
                {
                    if (_inner == null)
                    {
                        if (await _source.MoveNextAsync())
                        {
                            _inner = _mapper(_source.Current).GetEnumerator();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (_inner.MoveNext())
                    {
                        Current = _inner.Current;
                        return true;
                    }

                    _inner.Dispose();
                    _inner = null;
                }
            }
        }
    }
}
