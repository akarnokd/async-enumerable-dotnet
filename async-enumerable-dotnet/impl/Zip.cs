// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

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

        public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var enumerators = new IAsyncEnumerator<TSource>[_sources.Length];
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            for (var i = 0; i < _sources.Length; i++)
            {
                enumerators[i] = _sources[i].GetAsyncEnumerator(cts.Token);
            }
            return new ZipArrayEnumerator(enumerators, _zipper, cts);
        }

        private sealed class ZipArrayEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource>[] _enumerators;

            private readonly Func<TSource[], TResult> _zipper;

            private readonly ValueTask<bool>[] _tasks;

            private readonly CancellationTokenSource _tokenSources;

            public ZipArrayEnumerator(IAsyncEnumerator<TSource>[] enumerators, Func<TSource[], TResult> zipper,
                CancellationTokenSource tokenSource)
            {
                _enumerators = enumerators;
                _zipper = zipper;
                _tasks = new ValueTask<bool>[enumerators.Length];
                _tokenSources = tokenSource;
            }

            public TResult Current { get; private set; }

            public async ValueTask DisposeAsync()
            {
                _tokenSources.Cancel();
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
                    _tokenSources.Cancel();

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
