// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ToEnumerable<T> : IEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        public ToEnumerable(IAsyncEnumerable<T> source)
        {
            _source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ToEnumerator(_source.GetAsyncEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class ToEnumerator : IEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            public ToEnumerator(IAsyncEnumerator<T> source)
            {
                _source = source;
            }

            public T Current => _source.Current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _source.DisposeAsync().AsTask().Wait();
            }

            public bool MoveNext()
            {
                return _source.MoveNextAsync().AsTask().Result;
            }

            public void Reset()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
