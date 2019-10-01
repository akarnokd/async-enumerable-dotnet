// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Reduce<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<T, T, T> _reducer;

        public Reduce(IAsyncEnumerable<T> source, Func<T, T, T> reducer)
        {
            _source = source;
            _reducer = reducer;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ReduceEnumerator(_source.GetAsyncEnumerator(), _reducer);
        }

        private sealed class ReduceEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Func<T, T, T> _reducer;

            public T Current { get; private set; }

            private bool _once;

            public ReduceEnumerator(IAsyncEnumerator<T> source, Func<T, T, T> reducer)
            {
                _source = source;
                _reducer = reducer;
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    Current = default;
                    return false;
                }
                _once = true;

                var first = true;
                var accumulator = default(T);

                while (await _source.MoveNextAsync())
                {
                    if (first)
                    {
                        accumulator = _source.Current;
                        first = false;
                    }
                    else
                    {
                        accumulator = _reducer(accumulator, _source.Current);
                    }
                }

                if (first)
                {
                    return false;
                }
                Current = accumulator;
                return true;
            }
        }
    }

    internal sealed class ReduceSeed<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TResult> _initialSupplier;

        private readonly Func<TResult, TSource, TResult> _reducer;

        public ReduceSeed(IAsyncEnumerable<TSource> source, Func<TResult> initialSupplier, Func<TResult, TSource, TResult> reducer)
        {
            _source = source;
            _initialSupplier = initialSupplier;
            _reducer = reducer;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            TResult initial;
            try
            {
                initial = _initialSupplier();
            }
            catch (Exception ex)
            {
                return new Error<TResult>.ErrorEnumerator(ex);
            }
            return new ReduceEnumerator(_source.GetAsyncEnumerator(), initial, _reducer);
        }

        private sealed class ReduceEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TResult, TSource, TResult> _reducer;

            public TResult Current { get; private set; }

            private bool _once;

            public ReduceEnumerator(IAsyncEnumerator<TSource> source, TResult initial, Func<TResult, TSource, TResult> reducer)
            {
                _source = source;
                Current = initial;
                _reducer = reducer;
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    Current = default;
                    return false;
                }
                _once = true;

                while (await _source.MoveNextAsync())
                {
                    Current = _reducer(Current, _source.Current);
                }

                return true;
            }
        }
    }
}
