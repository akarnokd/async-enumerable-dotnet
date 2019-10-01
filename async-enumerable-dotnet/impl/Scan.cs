// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Scan<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<T, T, T> _scanner;

        public Scan(IAsyncEnumerable<T> source, Func<T, T, T> scanner)
        {
            _source = source;
            _scanner = scanner;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ScanEnumerator(_source.GetAsyncEnumerator(), _scanner);
        }

        private sealed class ScanEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly Func<T, T, T> _scanner;

            private bool _once;

            public T Current { get; private set; }

            public ScanEnumerator(IAsyncEnumerator<T> source, Func<T, T, T> scanner)
            {
                _source = source;
                _scanner = scanner;
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (!_once)
                {
                    _once = true;
                    if (await _source.MoveNextAsync())
                    {
                        Current = _source.Current;
                        return true;
                    }
                    return false;
                }
                if (await _source.MoveNextAsync())
                {
                    Current = _scanner(Current, _source.Current);
                    return true;
                }
                return false;
            }
        }
    }

    internal sealed class ScanSeed<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TResult> _initialSupplier;

        private readonly Func<TResult, TSource, TResult> _scanner;

        public ScanSeed(IAsyncEnumerable<TSource> source, Func<TResult> initialSupplier, Func<TResult, TSource, TResult> scanner)
        {
            _source = source;
            _initialSupplier = initialSupplier;
            _scanner = scanner;
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

            return new ScanSeedEnumerator(_source.GetAsyncEnumerator(), _scanner, initial);
        }

        private sealed class ScanSeedEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TResult, TSource, TResult> _scanner;

            public TResult Current { get; private set; }

            private bool _once;

            public ScanSeedEnumerator(IAsyncEnumerator<TSource> source, Func<TResult, TSource, TResult> scanner, TResult current)
            {
                _source = source;
                _scanner = scanner;
                Current = current;
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (!_once)
                {
                    _once = true;
                    return true;
                }

                if (await _source.MoveNextAsync())
                {
                    Current = _scanner(Current, _source.Current);
                    return true;
                }
                return false;
            }
        }
    }
}
