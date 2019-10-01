// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class All<TSource> : IAsyncEnumerable<bool>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, bool> _predicate;

        public All(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IAsyncEnumerator<bool> GetAsyncEnumerator()
        {
            return new AllEnumerator(_source.GetAsyncEnumerator(), _predicate);
        }

        private sealed class AllEnumerator : IAsyncEnumerator<bool>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, bool> _predicate;

            public bool Current { get; private set; }

            private bool _once;

            public AllEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, bool> predicate)
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
                    return false;
                }

                _once = true;

                while (await _source.MoveNextAsync())
                {
                    if (!_predicate(_source.Current))
                    {
                        Current = false;
                        return true;
                    }
                }

                Current = true;
                return true;
            }
        }
    }
    
    internal sealed class AllTask<TSource> : IAsyncEnumerable<bool>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, Task<bool>> _predicate;

        public AllTask(IAsyncEnumerable<TSource> source, Func<TSource, Task<bool>> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IAsyncEnumerator<bool> GetAsyncEnumerator()
        {
            return new AllTaskEnumerator(_source.GetAsyncEnumerator(), _predicate);
        }

        private sealed class AllTaskEnumerator : IAsyncEnumerator<bool>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, Task<bool>> _predicate;

            public bool Current { get; private set; }

            private bool _once;

            public AllTaskEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, Task<bool>> predicate)
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
                    return false;
                }

                _once = true;

                while (await _source.MoveNextAsync())
                {
                    if (!await _predicate(_source.Current))
                    {
                        Current = false;
                        return true;
                    }
                }

                Current = true;
                return true;
            }
        }
    }
}