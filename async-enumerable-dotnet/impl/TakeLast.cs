// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class TakeLast<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly int _n;

        public TakeLast(IAsyncEnumerable<T> source, int n)
        {
            _source = source;
            _n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new TakeLastEnumerator(_source.GetAsyncEnumerator(cancellationToken), _n);
        }

        private sealed class TakeLastEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private int _size;

            public T Current => _current;

            private ArrayQueue<T> _queue;

            private bool _once;

            private T _current;

            public TakeLastEnumerator(IAsyncEnumerator<T> source, int size)
            {
                _source = source;
                _size = size;
                _queue = new ArrayQueue<T>(16);
            }

            public ValueTask DisposeAsync()
            {
                if (_size == 0)
                {
                    _queue.Release();
                    return new ValueTask();
                }

                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (_once)
                    {
                        if (_queue.Dequeue(out _current))
                        {
                            return true;
                        }
                        return false;
                    }
                    while (await _source.MoveNextAsync())
                    {
                        if (_size != 0)
                        {
                            _size--;
                        }
                        else
                        {
                            _queue.Dequeue(out _);
                        }
                        _queue.Enqueue(_source.Current);
                    }
                    _once = true;
                }
            }
        }
    }
}
