// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SkipLast<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly int _n;

        public SkipLast(IAsyncEnumerable<T> source, int n)
        {
            _source = source;
            _n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new SkipLastEnumerable(_source.GetAsyncEnumerator(cancellationToken), _n);
        }

        private sealed class SkipLastEnumerable : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private int _size;

            private ArrayQueue<T> _queue;

            private T _current;

            public T Current => _current;

            public SkipLastEnumerable(IAsyncEnumerator<T> source, int n)
            {
                _source = source;
                _size = n;
                _queue = new ArrayQueue<T>(16);
            }

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                while (_size != 0)
                {
                    if (await _source.MoveNextAsync())
                    {
                        _queue.Enqueue(_source.Current);
                        _size--;
                    }
                    else
                    {
                        _size = 0;
                        return false;
                    }
                }

                if (await _source.MoveNextAsync())
                {
                    _queue.Dequeue(out _current);
                    _queue.Enqueue(_source.Current);
                    return true;
                }
                return false;
            }
        }
    }
}
