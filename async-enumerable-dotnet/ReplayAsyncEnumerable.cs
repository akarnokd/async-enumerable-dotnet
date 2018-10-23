using async_enumerable_dotnet.impl;
using System;
using System.Collections.Generic;
using System.Text;
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
        readonly IBufferManager buffer;

        ReplayEnumerator[] enumerators;

        static readonly ReplayEnumerator[] EMPTY = new ReplayEnumerator[0];

        static readonly ReplayEnumerator[] TERMINATED = new ReplayEnumerator[0];

        /// <summary>
        /// Returns true if there are any consumers to this AsyncEnumerable.
        /// </summary>
        public bool HasConsumers => enumerators.Length != 0;

        static readonly Func<long> DefaultTimeSource = () =>
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        };

        /// <summary>
        /// Construct an unbounded ReplayAsyncEnumerable.
        /// </summary>
        public ReplayAsyncEnumerable()
        {
            this.buffer = new UnboundedBuffer();
            Volatile.Write(ref enumerators, EMPTY);
        }

        /// <summary>
        /// Construct a size-bound ReplayAsyncEnumerable.
        /// </summary>
        /// <param name="maxSize">The maximum number of items to retain.</param>
        public ReplayAsyncEnumerable(int maxSize)
        {
            this.buffer = new SizeBoundBuffer(maxSize);
            Volatile.Write(ref enumerators, EMPTY);
        }

        /// <summary>
        /// Construct a time-bound ReplayAsyncEnumerable.
        /// </summary>
        /// <param name="maxAge">The maximum age of items to retain.</param>
        /// <param name="timeSource">The optional source for the current time. Defaults to the unix epoch milliseconds.</param>
        public ReplayAsyncEnumerable(TimeSpan maxAge, Func<long> timeSource = null)
        {
            this.buffer = new TimeSizeBoundBuffer(int.MaxValue, maxAge, timeSource ?? DefaultTimeSource);
            Volatile.Write(ref enumerators, EMPTY);
        }

        /// <summary>
        /// Construct a time- and size-bound ReplayAsyncEnumerable.
        /// </summary>
        /// <param name="maxSize">The maximum number of items to retain.</param>
        /// <param name="maxAge">The maximum age of items to retain.</param>
        /// <param name="timeSource">The optional source for the current time. Defaults to the unix epoch milliseconds.</param>
        public ReplayAsyncEnumerable(int maxSize, TimeSpan maxAge, Func<long> timeSource = null)
        {
            this.buffer = new TimeSizeBoundBuffer(maxSize, maxAge, timeSource ?? DefaultTimeSource);
            Volatile.Write(ref enumerators, EMPTY);
        }

        /// <summary>
        /// Indicate no more items will be pushed. Can be called at most once.
        /// </summary>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public ValueTask Complete()
        {
            buffer.Complete();
            foreach (var en in Interlocked.Exchange(ref enumerators, TERMINATED))
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
            buffer.Error(ex);
            foreach (var en in Interlocked.Exchange(ref enumerators, TERMINATED))
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
            buffer.Next(value);
            foreach (var en in Volatile.Read(ref enumerators))
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

        internal bool Add(ReplayEnumerator inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref enumerators);
                if (a == TERMINATED)
                {
                    return false;
                }
                var n = a.Length;
                var b = new ReplayEnumerator[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = inner;
                if (Interlocked.CompareExchange(ref enumerators, b, a) == a)
                {
                    return true;
                }
            }
        }

        internal void Remove(ReplayEnumerator inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref enumerators);
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

                var b = default(ReplayEnumerator[]);
                if (n == 1)
                {
                    b = EMPTY;
                }
                else
                {
                    b = new ReplayEnumerator[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref enumerators, b, a) == a)
                {
                    return;
                }
            }
        }

        internal interface IBufferManager
        {
            void Next(T item);

            void Error(Exception error);

            void Complete();

            ValueTask<bool> Drain(ReplayEnumerator consumer);
        }

        internal sealed class ReplayEnumerator : IAsyncEnumerator<T>
        {
            readonly ReplayAsyncEnumerable<T> parent;

            readonly IBufferManager buffer;

            internal int index;

            internal object node;

            internal long wip;

            internal T current;

            internal TaskCompletionSource<bool> resume;

            public T Current => current;

            public ReplayEnumerator(ReplayAsyncEnumerable<T> parent)
            {
                this.parent = parent;
                this.buffer = parent.buffer;
            }

            public ValueTask DisposeAsync()
            {
                current = default;
                node = null;
                parent.Remove(this);
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return buffer.Drain(this);
            }

            public void Signal()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    ResumeHelper.Resume(ref resume).TrySetResult(true);
                }
            }
        }

        internal sealed class UnboundedBuffer : IBufferManager
        {
            readonly IList<T> values;

            volatile int size;
            Exception error;
            volatile bool done;

            public UnboundedBuffer()
            {
                this.values = new List<T>();
            }

            public void Complete()
            {
                this.done = true;
            }

            public async ValueTask<bool> Drain(ReplayEnumerator consumer)
            {
                for (; ; )
                {
                    var d = done;
                    var s = size;
                    var index = consumer.index;

                    if (d && s == index)
                    {
                        if (error != null)
                        {
                            throw error;
                        }
                        return false;
                    }

                    if (index != s)
                    {
                        consumer.current = values[index];
                        consumer.index = index + 1;
                        return true;
                    }

                    if (Volatile.Read(ref consumer.wip) == 0)
                    {
                        await ResumeHelper.Resume(ref consumer.resume).Task;
                    }
                    ResumeHelper.Clear(ref consumer.resume);
                    Interlocked.Exchange(ref consumer.wip, 0L);
                }
            }

            public void Error(Exception error)
            {
                this.error = error;
                this.done = true;
            }

            public void Next(T item)
            {
                values.Add(item);
                size++;
            }
        }

        internal sealed class SizeBoundBuffer : IBufferManager
        {
            readonly int maxSize;

            Exception error;
            volatile bool done;

            volatile Node head;
            Node tail;

            int size;

            internal SizeBoundBuffer(int maxSize)
            {
                this.maxSize = maxSize;
                var h = new Node(default);
                tail = h;
                head = h;
            }

            internal void ClearHead()
            {
                // clear any beyond max size items by replacing the head
                var h = head;
                if (h.next != null)
                {
                    var n = new Node(default);
                    n.next = h.next;
                    head = n;
                }
            }

            public void Complete()
            {
                ClearHead();
                done = true;
            }

            public void Error(Exception error)
            {
                ClearHead();
                this.error = error;
                this.done = true;
            }

            public void Next(T item)
            {
                var n = new Node(item);
                if (size != maxSize)
                {
                    size++;
                    tail.next = n;
                    tail = n;
                }
                else
                {
                    tail.next = n;
                    tail = n;
                    head = head.next;
                }
            }

            public async ValueTask<bool> Drain(ReplayEnumerator consumer)
            {
                for (; ;)
                {
                    var d = done;
                    var n = consumer.node as Node;
                    if (n == null)
                    {
                        n = head;
                        consumer.node = head;
                    }
                    var x = n.next;
                    var empty = x == null;
                    if (d && empty)
                    {
                        if (error != null)
                        {
                            throw error;
                        }
                        return false;
                    }
                    else if (!empty)
                    {
                        consumer.current = x.item;
                        consumer.node = x;
                        return true;
                    }

                    if (Volatile.Read(ref consumer.wip) == 0)
                    {
                        await ResumeHelper.Resume(ref consumer.resume).Task;
                    }
                    ResumeHelper.Clear(ref consumer.resume);
                    Interlocked.Exchange(ref consumer.wip, 0L);
                }
            }

            internal sealed class Node
            {
                internal readonly T item;

                internal volatile Node next;

                internal Node(T item)
                {
                    this.item = item;
                }
            }
        }

        internal sealed class TimeSizeBoundBuffer : IBufferManager
        {
            readonly long maxAge;

            readonly int maxSize;

            readonly Func<long> timeSource;

            Exception error;
            volatile bool done;

            volatile Node head;
            Node tail;

            int size;

            internal TimeSizeBoundBuffer(int maxSize, TimeSpan maxAge, Func<long> timeSource)
            {
                this.maxSize = maxSize;
                this.maxAge = (long)maxAge.TotalMilliseconds;
                this.timeSource = timeSource;
                var h = new Node(default, 0L);
                tail = h;
                head = h;
            }

            internal void ClearHead()
            {
                // clear any beyond max size items by replacing the head
                if (head.next != null)
                {
                    var n = new Node(default, 0L);
                    n.next = head.next;
                    head = n;
                }
            }

            void TrimTime()
            {
                long now = timeSource() - maxAge;

                var c = size;
                var h = head;
                for (; ;)
                {
                    var x = h.next;
                    if (x == null)
                    {
                        break;
                    }
                    if (x.timestamp > now)
                    {
                        break;
                    }
                    h = x;
                    c--;
                }

                if (h != head)
                {
                    size = c;
                    head = h;
                }
            }

            Node FindHead()
            {
                long now = timeSource() - maxAge;

                var h = head;
                for (; ; )
                {
                    var x = h.next;
                    if (x == null)
                    {
                        return h;
                    }
                    if (x.timestamp > now)
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
                done = true;
            }

            public void Error(Exception error)
            {
                TrimTime();
                ClearHead();
                this.error = error;
                this.done = true;
            }

            public void Next(T item)
            {
                var n = new Node(item, timeSource());
                if (size != maxSize)
                {
                    size++;
                    tail.next = n;
                    tail = n;
                }
                else
                {
                    tail.next = n;
                    tail = n;
                    head = head.next;
                    TrimTime();
                }
            }

            public async ValueTask<bool> Drain(ReplayEnumerator consumer)
            {
                for (; ; )
                {
                    var d = done;
                    var n = consumer.node as Node;
                    if (n == null)
                    {
                        n = FindHead();
                        consumer.node = head;
                    }
                    var x = n.next;
                    var empty = x == null;
                    if (d && empty)
                    {
                        if (error != null)
                        {
                            throw error;
                        }
                        return false;
                    }
                    else if (!empty)
                    {
                        consumer.current = x.item;
                        consumer.node = x;
                        return true;
                    }

                    if (Volatile.Read(ref consumer.wip) == 0)
                    {
                        await ResumeHelper.Resume(ref consumer.resume).Task;
                    }
                    ResumeHelper.Clear(ref consumer.resume);
                    Interlocked.Exchange(ref consumer.wip, 0L);
                }
            }

            internal sealed class Node
            {
                internal readonly T item;

                internal readonly long timestamp;

                internal volatile Node next;

                internal Node(T item, long timestamp)
                {
                    this.item = item;
                    this.timestamp = timestamp;
                }
            }
        }

    }
}
