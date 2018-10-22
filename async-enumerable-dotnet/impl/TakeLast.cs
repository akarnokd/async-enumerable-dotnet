using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class TakeLast<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly int n;

        public TakeLast(IAsyncEnumerable<T> source, int n)
        {
            this.source = source;
            this.n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TakeLastEnumerator(source.GetAsyncEnumerator(), n);
        }

        internal sealed class TakeLastEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            int size;

            public T Current => current;

            ArrayQueue<T> queue;

            bool once;

            T current;

            public TakeLastEnumerator(IAsyncEnumerator<T> source, int size)
            {
                this.source = source;
                this.size = size;
                this.queue = new ArrayQueue<T>(16);
            }

            public ValueTask DisposeAsync()
            {
                if (size == 0)
                {
                    queue.Release();
                    return new ValueTask();
                }

                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (once)
                    {
                        if (queue.Dequeue(out current))
                        {
                            return true;
                        }
                        return false;
                    }
                    while (await source.MoveNextAsync())
                    {
                        if (size != 0)
                        {
                            size--;
                        }
                        else
                        {
                            queue.Dequeue(out _);
                        }
                        queue.Enqueue(source.Current);
                    }
                    once = true;
                }
            }
        }
    }
}
