// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class BufferExact<TSource, TCollection> : IAsyncEnumerable<TCollection> where TCollection : ICollection<TSource>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly int _n;

        private readonly Func<TCollection> _bufferSupplier;

        public BufferExact(IAsyncEnumerable<TSource> source, int n, Func<TCollection> bufferSupplier)
        {
            _source = source;
            _n = n;
            _bufferSupplier = bufferSupplier;
        }

        public IAsyncEnumerator<TCollection> GetAsyncEnumerator()
        {
            return new BufferExactEnumerator(_source.GetAsyncEnumerator(), _n, _bufferSupplier);
        }

        private sealed class BufferExactEnumerator : IAsyncEnumerator<TCollection>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly int _n;

            private readonly Func<TCollection> _bufferSupplier;

            public TCollection Current { get; private set; }

            private int _count;

            private bool _done;

            public BufferExactEnumerator(IAsyncEnumerator<TSource> source, int n, Func<TCollection> bufferSupplier)
            {
                _source = source;
                _n = n;
                _bufferSupplier = bufferSupplier;
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
                var buf = _bufferSupplier();

                while (_count != _n)
                {
                    if (await _source.MoveNextAsync())
                    {
                        buf.Add(_source.Current);
                        _count++;
                    }
                    else
                    {
                        _done = true;
                        break;
                    }
                }

                if (_count == 0)
                {
                    return false;
                }

                _count = 0;
                Current = buf;
                return true;
            }
        }
    }

    internal sealed class BufferSkip<TSource, TCollection> : IAsyncEnumerable<TCollection> where TCollection : ICollection<TSource>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly int _size;

        private readonly int _skip;

        private readonly Func<TCollection> _bufferSupplier;

        public BufferSkip(IAsyncEnumerable<TSource> source, int size, int skip, Func<TCollection> bufferSupplier)
        {
            _source = source;
            _size = size;
            _skip = skip;
            _bufferSupplier = bufferSupplier;
        }

        public IAsyncEnumerator<TCollection> GetAsyncEnumerator()
        {
            return new BufferSkipEnumerator(_source.GetAsyncEnumerator(), _size, _skip, _bufferSupplier);
        }

        private sealed class BufferSkipEnumerator : IAsyncEnumerator<TCollection>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly int _size;

            private readonly int _skip;

            private readonly Func<TCollection> _bufferSupplier;

            public TCollection Current { get; private set; }

            private int _count;

            private bool _once;

            public BufferSkipEnumerator(IAsyncEnumerator<TSource> source, int size, int skip, Func<TCollection> bufferSupplier)
            {
                _source = source;
                _size = size;
                _skip = skip;
                _bufferSupplier = bufferSupplier;
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    while (_count++ != _skip)
                    {
                        if (!await _source.MoveNextAsync())
                        {
                            return false;
                        }
                    }
                }

                _once = true;

                var buf = _bufferSupplier();
                _count = 0;
                while (_count != _size)
                {
                    if (await _source.MoveNextAsync())
                    {
                        buf.Add(_source.Current);
                        _count++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (_count == 0)
                {
                    return false;
                }

                Current = buf;
                return true;
            }
        }
    }

    internal sealed class BufferOverlap<TSource, TCollection> : IAsyncEnumerable<TCollection> where TCollection : ICollection<TSource>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly int _size;

        private readonly int _skip;

        private readonly Func<TCollection> _bufferSupplier;

        public BufferOverlap(IAsyncEnumerable<TSource> source, int size, int skip, Func<TCollection> bufferSupplier)
        {
            _source = source;
            _size = size;
            _skip = skip;
            _bufferSupplier = bufferSupplier;
        }

        public IAsyncEnumerator<TCollection> GetAsyncEnumerator()
        {
            return new BufferOverlapEnumerator(_source.GetAsyncEnumerator(), _size, _skip, _bufferSupplier);
        }

        private sealed class BufferOverlapEnumerator : IAsyncEnumerator<TCollection>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly int _size;

            private readonly int _skip;

            private readonly Func<TCollection> _bufferSupplier;

            private TCollection _buffer;

            public TCollection Current => _buffer;

            private int _index;

            private int _count;

            private bool _done;

            private ArrayQueue<TCollection> _buffers;

            public BufferOverlapEnumerator(IAsyncEnumerator<TSource> source, int size, int skip, Func<TCollection> bufferSupplier)
            {
                _source = source;
                _size = size;
                _skip = skip;
                _bufferSupplier = bufferSupplier;
                _buffers = new ArrayQueue<TCollection>(16);
            }

            public ValueTask DisposeAsync()
            {
                _buffer = default;
                _buffers.Release();
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (_done)
                    {
                        if (_buffers.Dequeue(out _buffer))
                        {
                            return true;
                        }
                        return false;
                    }


                    if (await _source.MoveNextAsync())
                    {
                        if (_index == 0)
                        {
                            _buffers.Enqueue(_bufferSupplier());
                        }

                        _index++;
                        _count++;
                        _buffers.ForEach(_source.Current, (b, v) => b.Add(v));
                    }
                    else
                    {
                        _done = true;
                        continue;
                    }

                    if (_index == _skip)
                    {
                        _index = 0;
                    }
                    if (_count == _size)
                    {
                        _count -= _skip;
                        _buffers.Dequeue(out _buffer);
                        return true;
                    }
                }
            }
        }
    }
}
