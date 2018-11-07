// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Any<TSource> : IAsyncEnumerable<bool>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, bool> _predicate;

        public Any(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IAsyncEnumerator<bool> GetAsyncEnumerator()
        {
            return new AnyEnumerator(_source.GetAsyncEnumerator(), _predicate);
        }

        private sealed class AnyEnumerator : IAsyncEnumerator<bool>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, bool> _predicate;

            public bool Current { get; private set; }

            private bool _once;

            public AnyEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, bool> predicate)
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
                    if (_predicate(_source.Current))
                    {
                        Current = true;
                        return true;
                    }
                }

                Current = false;
                return true;
            }
        }
    }
    
    internal sealed class AnyTask<TSource> : IAsyncEnumerable<bool>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, Task<bool>> _predicate;

        public AnyTask(IAsyncEnumerable<TSource> source, Func<TSource, Task<bool>> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IAsyncEnumerator<bool> GetAsyncEnumerator()
        {
            return new AnyTaskEnumerator(_source.GetAsyncEnumerator(), _predicate);
        }

        private sealed class AnyTaskEnumerator : IAsyncEnumerator<bool>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, Task<bool>> _predicate;

            public bool Current { get; private set; }

            private bool _once;

            public AnyTaskEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, Task<bool>> predicate)
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
                    if (await _predicate(_source.Current))
                    {
                        Current = true;
                        return true;
                    }
                }

                Current = false;
                return true;
            }
        }
    }
}