// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ElementAt<TSource> : IAsyncEnumerable<TSource>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly long _index;

        private readonly TSource _defaultItem;

        private readonly bool _hasDefault;

        public ElementAt(IAsyncEnumerable<TSource> source, long index, TSource defaultItem, bool hasDefault)
        {
            _source = source;
            _index = index;
            _defaultItem = defaultItem;
            _hasDefault = hasDefault;
        }

        public IAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new ElementAtEnumerator(_source.GetAsyncEnumerator(cancellationToken), _index, _defaultItem, _hasDefault);
        }

        private sealed class ElementAtEnumerator : IAsyncEnumerator<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly long _index;

            private readonly TSource _defaultItem;

            private readonly bool _hasDefault;

            public TSource Current { get; private set; }

            private bool _done;
            
            public ElementAtEnumerator(IAsyncEnumerator<TSource> source, long index, TSource defaultItem, bool hasDefault)
            {
                _source = source;
                _index = index;
                _defaultItem = defaultItem;
                _hasDefault = hasDefault;
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_done)
                {
                    return false;
                }
                var idx = 0L;
                for (;;)
                {
                    if (await _source.MoveNextAsync())
                    {
                        if (idx == _index)
                        {
                            Current = _source.Current;
                            _done = true;
                            return true;
                        }

                        idx++;
                    }
                    else
                    {
                        if (_hasDefault)
                        {
                            Current = _defaultItem;
                            _done = true;
                            return true;
                        }
                        
                        throw new IndexOutOfRangeException();
                    }
                }
            }
        }
    }
}