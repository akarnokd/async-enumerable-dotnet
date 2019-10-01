// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

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
            var cts = new CancellationTokenSource();
            return new ToEnumerator(_source.GetAsyncEnumerator(cts.Token), cts);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class ToEnumerator : IEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly CancellationTokenSource _cts;

            public ToEnumerator(IAsyncEnumerator<T> source, CancellationTokenSource cts)
            {
                _source = source;
                _cts = cts;
            }

            public T Current => _source.Current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _cts.Cancel();
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
