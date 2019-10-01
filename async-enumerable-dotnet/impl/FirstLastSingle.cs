// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class First<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly T _defaultItem;

        private readonly bool _hasDefault;

        public First(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            _source = source;
            _defaultItem = defaultItem;
            _hasDefault = hasDefault;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new FirstEnumerator(_source.GetAsyncEnumerator(cancellationToken), _defaultItem, _hasDefault);
        }

        private sealed class FirstEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly T _defaultItem;

            private readonly bool _hasDefault;

            public T Current { get; private set; }

            private bool _done;

            public FirstEnumerator(IAsyncEnumerator<T> source, T defaultItem, bool hasDefault)
            {
                _source = source;
                _defaultItem = defaultItem;
                _hasDefault = hasDefault;
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_done)
                {
                    return false;
                }
                _done = true;

                if (await _source.MoveNextAsync())
                {
                    Current = _source.Current;
                    return true;
                }
                if (_hasDefault)
                {
                    Current = _defaultItem;
                    return true;
                }
                throw new IndexOutOfRangeException("The source is empty");
            }
        }
    }

    internal sealed class Last<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly T _defaultItem;

        private readonly bool _hasDefault;

        public Last(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            _source = source;
            _defaultItem = defaultItem;
            _hasDefault = hasDefault;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new LastEnumerator(_source.GetAsyncEnumerator(cancellationToken), _defaultItem, _hasDefault);
        }

        private sealed class LastEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly T _defaultItem;

            private readonly bool _hasDefault;

            public T Current { get; private set; }

            private bool _done;

            public LastEnumerator(IAsyncEnumerator<T> source, T defaultItem, bool hasDefault)
            {
                _source = source;
                _defaultItem = defaultItem;
                _hasDefault = hasDefault;
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_done)
                {
                    return false;
                }
                _done = true;
                var hasValue = false;
                var last = default(T); 
                while (await _source.MoveNextAsync())
                {
                    hasValue = true;
                    last = _source.Current;
                }

                if (hasValue)
                {
                    Current = last;
                    return true;
                }
                if (_hasDefault)
                {
                    Current = _defaultItem;
                    return true;
                }
                throw new IndexOutOfRangeException("The source is empty");
            }
        }
    }

    internal sealed class Single<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly T _defaultItem;

        private readonly bool _hasDefault;

        public Single(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            _source = source;
            _defaultItem = defaultItem;
            _hasDefault = hasDefault;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new SingleEnumerator(_source.GetAsyncEnumerator(cancellationToken), _defaultItem, _hasDefault);
        }

        private sealed class SingleEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly T _defaultItem;

            private readonly bool _hasDefault;

            public T Current { get; private set; }

            private bool _done;

            public SingleEnumerator(IAsyncEnumerator<T> source, T defaultItem, bool hasDefault)
            {
                _source = source;
                _defaultItem = defaultItem;
                _hasDefault = hasDefault;
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_done)
                {
                    return false;
                }
                _done = true;

                if (await _source.MoveNextAsync())
                {
                    var single = _source.Current;
                    if (await _source.MoveNextAsync())
                    {
                        throw new IndexOutOfRangeException("The source has more than one item");
                    }
                    Current = single;
                    return true;
                }

                if (_hasDefault)
                {
                    Current = _defaultItem;
                    return true;
                }
                throw new IndexOutOfRangeException("The source is empty");
            }
        }
    }

}
