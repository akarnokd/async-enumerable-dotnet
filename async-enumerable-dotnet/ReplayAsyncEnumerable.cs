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

        public bool HasConsumers => enumerators.Length != 0;

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
            // TODO
        }

        public ReplayAsyncEnumerable(TimeSpan maxAge)
        {
            // TODO
        }

        public ReplayAsyncEnumerable(int maxSize, TimeSpan maxAge)
        {
            // TODO
        }

        public ValueTask Complete()
        {
            buffer.Complete();
            foreach (var en in Interlocked.Exchange(ref enumerators, TERMINATED))
            {
                en.Signal();
            }
            return new ValueTask();
        }

        public ValueTask Error(Exception ex)
        {
            buffer.Error(ex);
            foreach (var en in Interlocked.Exchange(ref enumerators, TERMINATED))
            {
                en.Signal();
            }
            return new ValueTask();
        }

        public ValueTask Next(T value)
        {
            buffer.Next(value);
            foreach (var en in Volatile.Read(ref enumerators))
            {
                en.Signal();
            }
            return new ValueTask();
        }

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
                        ResumeHelper.Clear(ref consumer.resume);
                        Interlocked.Decrement(ref consumer.wip);
                        return true;
                    }

                    if (Volatile.Read(ref consumer.wip) == 0)
                    {
                        await ResumeHelper.Resume(ref consumer.resume).Task;
                        ResumeHelper.Clear(ref consumer.resume);
                    }
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
                if (head.next != null)
                {
                    var n = new Node(default);
                    n.next = head.next;
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
                throw new NotImplementedException();
            }

            public void Next(T item)
            {
                throw new NotImplementedException();
            }

            public ValueTask<bool> Drain(ReplayEnumerator consumer)
            {
                throw new NotImplementedException();
            }

            internal sealed class Node
            {
                internal readonly T item;

                internal Node next;

                internal Node(T item)
                {
                    this.item = item;
                }
            }
        }
    }
}
