// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using async_enumerable_dotnet.impl;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// Caches items and terminal signals and replays it
    /// to its IAsyncEnumerator consumers.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class ReplayAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncConsumer<T>
    {
        private readonly IBufferManager _buffer;

        private ReplayEnumerator[] _enumerators;

        private static readonly ReplayEnumerator[] Empty = new ReplayEnumerator[0];

        private static readonly ReplayEnumerator[] Terminated = new ReplayEnumerator[0];

        /// <summary>
        /// Returns true if there are any consumers to this AsyncEnumerable.
        /// </summary>
        public bool HasConsumers => _enumerators.Length != 0;

        /// <summary>
        /// Construct an unbounded ReplayAsyncEnumerable.
        /// </summary>
        public ReplayAsyncEnumerable()
        {
            _buffer = new UnboundedBuffer();
            Volatile.Write(ref _enumerators, Empty);
        }

        /// <summary>
        /// Construct a size-bound ReplayAsyncEnumerable.
        /// </summary>
        /// <param name="maxSize">The maximum number of items to retain.</param>
        public ReplayAsyncEnumerable(int maxSize)
        {
            _buffer = new SizeBoundBuffer(maxSize);
            Volatile.Write(ref _enumerators, Empty);
        }

        /// <summary>
        /// Construct a time-bound ReplayAsyncEnumerable.
        /// </summary>
        /// <param name="maxAge">The maximum age of items to retain.</param>
        /// <param name="timeSource">The optional source for the current time. Defaults to the Unix epoch milliseconds.</param>
        public ReplayAsyncEnumerable(TimeSpan maxAge, Func<long> timeSource = null)
        {
            _buffer = new TimeSizeBoundBuffer(int.MaxValue, maxAge, timeSource ?? TimeSource.DefaultTimeSource);
            Volatile.Write(ref _enumerators, Empty);
        }

        /// <summary>
        /// Construct a time- and size-bound ReplayAsyncEnumerable.
        /// </summary>
        /// <param name="maxSize">The maximum number of items to retain.</param>
        /// <param name="maxAge">The maximum age of items to retain.</param>
        /// <param name="timeSource">The optional source for the current time. Defaults to the Unix epoch milliseconds.</param>
        public ReplayAsyncEnumerable(int maxSize, TimeSpan maxAge, Func<long> timeSource = null)
        {
            _buffer = new TimeSizeBoundBuffer(maxSize, maxAge, timeSource ?? TimeSource.DefaultTimeSource);
            Volatile.Write(ref _enumerators, Empty);
        }

        /// <summary>
        /// Indicate no more items will be pushed. Can be called at most once.
        /// </summary>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public ValueTask Complete()
        {
            _buffer.Complete();
            foreach (var en in Interlocked.Exchange(ref _enumerators, Terminated))
            {
                en.Signal();
            }
            return new ValueTask();
        }

        /// <summary>
        /// Push a final exception. Can be called at most once.
        /// </summary>
        /// <param name="ex">The exception to push.</param>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public ValueTask Error(Exception ex)
        {
            _buffer.Error(ex);
            foreach (var en in Interlocked.Exchange(ref _enumerators, Terminated))
            {
                en.Signal();
            }
            return new ValueTask();
        }

        /// <summary>
        /// Push a value. Can be called multiple times.
        /// </summary>
        /// <param name="value">The value to push.</param>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public ValueTask Next(T value)
        {
            _buffer.Next(value);
            foreach (var en in Volatile.Read(ref _enumerators))
            {
                en.Signal();
            }
            return new ValueTask();
        }

        /// <summary>
        /// Returns an <see cref="IAsyncEnumerator{T}"/> representing an active asynchronous sequence.
        /// </summary>
        /// <returns>The active asynchronous sequence to be consumed.</returns>
        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new ReplayEnumerator(this);
            Add(en);
            return en;
        }

        private void Add(ReplayEnumerator inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref _enumerators);
                if (a == Terminated)
                {
                    return;
                }
                var n = a.Length;
                var b = new ReplayEnumerator[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = inner;
                if (Interlocked.CompareExchange(ref _enumerators, b, a) == a)
                {
                    return;
                }
            }
        }

        private void Remove(ReplayEnumerator inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref _enumerators);
                var n = a.Length;
                if (n == 0)
                {
                    return;
                }

                var j = Array.IndexOf(a, inner);

                if (j < 0)
                {
                    return;
                }

                ReplayEnumerator[] b;
                if (n == 1)
                {
                    b = Empty;
                }
                else
                {
                    b = new ReplayEnumerator[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref _enumerators, b, a) == a)
                {
                    return;
                }
            }
        }

        private interface IBufferManager
        {
            void Next(T item);

            void Error(Exception error);

            void Complete();

            ValueTask<bool> Drain(ReplayEnumerator consumer);
        }

        internal sealed class ReplayEnumerator : IAsyncEnumerator<T>
        {
            private readonly ReplayAsyncEnumerable<T> _parent;

            private readonly IBufferManager _buffer;

            internal int Index;

            internal object Node;

            internal TaskCompletionSource<bool> Resume;

            public T Current { get; internal set; }

            public ReplayEnumerator(ReplayAsyncEnumerable<T> parent)
            {
                _parent = parent;
                _buffer = parent._buffer;
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                Node = null;
                _parent.Remove(this);
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return _buffer.Drain(this);
            }

            public void Signal()
            {
                ResumeHelper.Resume(ref Resume);
            }
        }

        private sealed class UnboundedBuffer : IBufferManager
        {
            private readonly IList<T> _values;

            private volatile int _size;
            private Exception _error;
            private volatile bool _done;

            public UnboundedBuffer()
            {
                _values = new List<T>();
            }

            public void Complete()
            {
                _done = true;
            }

            public async ValueTask<bool> Drain(ReplayEnumerator consumer)
            {
                for (; ; )
                {
                    var d = _done;
                    var s = _size;
                    var index = consumer.Index;

                    if (d && s == index)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }

                    if (index != s)
                    {
                        consumer.Current = _values[index];
                        consumer.Index = index + 1;
                        return true;
                    }

                    await ResumeHelper.Await(ref consumer.Resume);
                    ResumeHelper.Clear(ref consumer.Resume);
                }
            }

            public void Error(Exception error)
            {
                _error = error;
                _done = true;
            }

            public void Next(T item)
            {
                _values.Add(item);
                _size++;
            }
        }

        internal sealed class SizeBoundBuffer : IBufferManager
        {
            private readonly int _maxSize;

            private Exception _error;
            private volatile bool _done;

            private volatile Node _head;
            private Node _tail;

            private int _size;

            internal SizeBoundBuffer(int maxSize)
            {
                _maxSize = maxSize;
                var h = new Node(default);
                _tail = h;
                _head = h;
            }

            private void ClearHead()
            {
                // clear any beyond max size items by replacing the head
                var h = _head;
                if (h.Next != null)
                {
                    var n = new Node(default)
                    {
                        Next = h.Next
                    };
                    _head = n;
                }
            }

            public void Complete()
            {
                ClearHead();
                _done = true;
            }

            public void Error(Exception error)
            {
                ClearHead();
                _error = error;
                _done = true;
            }

            public void Next(T item)
            {
                var n = new Node(item);
                if (_size != _maxSize)
                {
                    _size++;
                    _tail.Next = n;
                    _tail = n;
                }
                else
                {
                    _tail.Next = n;
                    _tail = n;
                    _head = _head.Next;
                }
            }

            public async ValueTask<bool> Drain(ReplayEnumerator consumer)
            {
                for (; ;)
                {
                    var d = _done;
                    var n = (Node)consumer.Node;
                    if (n == null)
                    {
                        n = _head;
                        consumer.Node = _head;
                    }
                    var x = n.Next;
                    var empty = x == null;
                    if (d && empty)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }

                    if (!empty)
                    {
                        consumer.Current = x.Item;
                        consumer.Node = x;
                        return true;
                    }

                    await ResumeHelper.Await(ref consumer.Resume);
                    ResumeHelper.Clear(ref consumer.Resume);
                }
            }

            internal sealed class Node
            {
                internal readonly T Item;

                internal volatile Node Next;

                internal Node(T item)
                {
                    Item = item;
                }
            }
        }

        internal sealed class TimeSizeBoundBuffer : IBufferManager
        {
            private readonly long _maxAge;

            private readonly int _maxSize;

            private readonly Func<long> _timeSource;

            private Exception _error;
            private volatile bool _done;

            private volatile Node _head;
            private Node _tail;

            private int _size;

            internal TimeSizeBoundBuffer(int maxSize, TimeSpan maxAge, Func<long> timeSource)
            {
                _maxSize = maxSize;
                _maxAge = (long)maxAge.TotalMilliseconds;
                _timeSource = timeSource;
                var h = new Node(default, 0L);
                _tail = h;
                _head = h;
            }

            private void ClearHead()
            {
                // clear any beyond max size items by replacing the head
                if (_head.Next != null)
                {
                    var n = new Node(default, 0L)
                    {
                        Next = _head.Next
                    };
                    _head = n;
                }
            }

            private void TrimTime()
            {
                var now = _timeSource() - _maxAge;

                var c = _size;
                var h = _head;
                for (; ;)
                {
                    var x = h.Next;
                    if (x == null)
                    {
                        break;
                    }
                    if (x.Timestamp > now)
                    {
                        break;
                    }
                    h = x;
                    c--;
                }

                if (h != _head)
                {
                    _size = c;
                    _head = h;
                }
            }

            private Node FindHead()
            {
                var now = _timeSource() - _maxAge;

                var h = _head;
                for (; ; )
                {
                    var x = h.Next;
                    if (x == null)
                    {
                        return h;
                    }
                    if (x.Timestamp > now)
                    {
                        return h;
                    }
                    h = x;
                }
            }

            public void Complete()
            {
                TrimTime();
                ClearHead();
                _done = true;
            }

            public void Error(Exception error)
            {
                TrimTime();
                ClearHead();
                _error = error;
                _done = true;
            }

            public void Next(T item)
            {
                var n = new Node(item, _timeSource());
                if (_size != _maxSize)
                {
                    _size++;
                    _tail.Next = n;
                    _tail = n;
                }
                else
                {
                    _tail.Next = n;
                    _tail = n;
                    _head = _head.Next;
                    TrimTime();
                }
            }

            public async ValueTask<bool> Drain(ReplayEnumerator consumer)
            {
                for (; ; )
                {
                    var d = _done;
                    var n = (Node)consumer.Node;
                    if (n == null)
                    {
                        n = FindHead();
                        consumer.Node = _head;
                    }
                    var x = n.Next;
                    var empty = x == null;
                    if (d && empty)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }

                    if (!empty)
                    {
                        consumer.Current = x.Item;
                        consumer.Node = x;
                        return true;
                    }

                    await ResumeHelper.Await(ref consumer.Resume);
                    ResumeHelper.Clear(ref consumer.Resume);
                }
            }

            internal sealed class Node
            {
                internal readonly T Item;

                internal readonly long Timestamp;

                internal volatile Node Next;

                internal Node(T item, long timestamp)
                {
                    Item = item;
                    Timestamp = timestamp;
                }
            }
        }

    }

    /// <summary>
    /// Hosts a singleton default time source function.
    /// </summary>
    internal static class TimeSource
    {
        internal static readonly Func<long> DefaultTimeSource = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
