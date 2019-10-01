// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ZipArray<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource>[] _sources;

        private readonly Func<TSource[], TResult> _zipper;

        public ZipArray(IAsyncEnumerable<TSource>[] sources, Func<TSource[], TResult> zipper)
        {
            _sources = sources;
            _zipper = zipper;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            var enumerators = new IAsyncEnumerator<TSource>[_sources.Length];
            for (var i = 0; i < _sources.Length; i++)
            {
                enumerators[i] = _sources[i].GetAsyncEnumerator();
            }
            return new ZipArrayEnumerator(enumerators, _zipper);
        }

        private sealed class ZipArrayEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource>[] _enumerators;

            private readonly Func<TSource[], TResult> _zipper;

            private readonly ValueTask<bool>[] _tasks;

            public ZipArrayEnumerator(IAsyncEnumerator<TSource>[] enumerators, Func<TSource[], TResult> zipper)
            {
                _enumerators = enumerators;
                _zipper = zipper;
                _tasks = new ValueTask<bool>[enumerators.Length];
            }

            public TResult Current { get; private set; }

            public async ValueTask DisposeAsync()
            {
                foreach (var en in _enumerators)
                {
                    await en.DisposeAsync().ConfigureAwait(false);
                }
                Current = default;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                var values = new TSource[_enumerators.Length];

                for (var i = 0; i < _enumerators.Length; i++)
                {
                    _tasks[i] = _enumerators[i].MoveNextAsync();
                }

                var fullRow = true;
                var errors = default(Exception);

                for (var i = 0; i < _tasks.Length; i++)
                {
                    try
                    {
                        if (await _tasks[i].ConfigureAwait(false))
                        {
                            values[i] = _enumerators[i].Current;
                        }
                        else
                        {
                            fullRow = false;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        fullRow = false;
                        errors = errors == null ? ex : new AggregateException(errors, ex);
                    }
                }

                if (!fullRow)
                {
                    if (errors != null)
                    {
                        throw errors;
                    }

                    return false;
                }

                Current = _zipper(values);
                return true;
            }
        }
    }
}
