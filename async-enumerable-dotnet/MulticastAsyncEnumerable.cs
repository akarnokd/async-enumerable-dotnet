using async_enumerable_dotnet.impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// Delivers items or terminal events to multiple
    /// consumers.
    /// </summary>
    /// <typeparam name="T">The element type of the async sequence.</typeparam>
    public sealed class MulticastAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncConsumer<T>
    {
        MulticastEnumerator[] enumerators;

        Exception error;

        static readonly MulticastEnumerator[] EMPTY = new MulticastEnumerator[0];

        static readonly MulticastEnumerator[] TERMINATED = new MulticastEnumerator[0];

        public bool HasConsumers => enumerators.Length != 0;

        public MulticastAsyncEnumerable()
        {
            Volatile.Write(ref enumerators, EMPTY);
        }

        public async ValueTask Next(T item)
        {
            foreach (var inner in Volatile.Read(ref enumerators))
            {
                await inner.Next(item);
            }
        }

        public async ValueTask Error(Exception error)
        {
            this.error = error;
            foreach (var inner in Interlocked.Exchange(ref enumerators, TERMINATED))
            {
                await inner.Error(error);
            }
        }

        public async ValueTask Complete()
        {
            foreach (var inner in Interlocked.Exchange(ref enumerators, TERMINATED))
            {
                await inner.Complete();
            }
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new MulticastEnumerator(this);
            if (!Add(en))
            {
                if (error != null)
                {
                    return new Error<T>.ErrorEnumerator(error);
                }
                return new Empty<T>();
            }
            return en;
        }

        internal bool Add(MulticastEnumerator inner)
        {
            for (; ;)
            {
                var a = Volatile.Read(ref enumerators);
                if (a == TERMINATED)
                {
                    return false;
                }
                var n = a.Length;
                var b = new MulticastEnumerator[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = inner;
                if (Interlocked.CompareExchange(ref enumerators, b, a) == a)
                {
                    return true;
                }
            }
        }

        internal void Remove(MulticastEnumerator inner)
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

                var b = default(MulticastEnumerator[]);
                if (n == 1)
                {
                    b = EMPTY;
                }
                else
                {
                    b = new MulticastEnumerator[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref enumerators, b, a) == a)
                {
                    return;
                }
            }
        }

        internal sealed class MulticastEnumerator : IAsyncEnumerator<T>, IAsyncConsumer<T>
        {
            readonly MulticastAsyncEnumerable<T> parent;

            T value;
            bool done;
            Exception error;

            TaskCompletionSource<bool> consumed;

            TaskCompletionSource<bool> valueReady;

            T current;

            public MulticastEnumerator(MulticastAsyncEnumerable<T> parent)
            {
                this.parent = parent;
                var tcs = new TaskCompletionSource<bool>();
                tcs.TrySetResult(true);
                Volatile.Write(ref consumed, tcs);
            }

            public T Current => current;

            public async ValueTask Complete()
            {
                await ResumeHelper.Resume(ref consumed).Task;
                ResumeHelper.Clear(ref consumed);

                this.done = true;

                ResumeHelper.Resume(ref valueReady).TrySetResult(true);
            }

            public ValueTask DisposeAsync()
            {
                current = default;
                parent.Remove(this);
                // unblock any Next/Error/Complete waiting for consumption
                ResumeHelper.Resume(ref consumed).TrySetResult(true);
                return new ValueTask();
            }

            public async ValueTask Error(Exception ex)
            {
                await ResumeHelper.Resume(ref consumed).Task;
                ResumeHelper.Clear(ref consumed);

                this.error = ex;

                ResumeHelper.Resume(ref valueReady).TrySetResult(true);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                await ResumeHelper.Resume(ref valueReady).Task;
                ResumeHelper.Clear(ref valueReady);

                if (error != null)
                {
                    throw error;
                }
                else
                if (done)
                {
                    return false;
                }

                current = value;
                value = default;
                ResumeHelper.Resume(ref consumed).TrySetResult(true);
                return true;
            }

            public async ValueTask Next(T value)
            {
                await ResumeHelper.Resume(ref consumed).Task;
                ResumeHelper.Clear(ref consumed);

                this.value = value;

                ResumeHelper.Resume(ref valueReady).TrySetResult(true);
            }
        }
    }
}
