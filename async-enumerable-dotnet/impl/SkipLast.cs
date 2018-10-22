using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SkipLast<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly int n;

        public SkipLast(IAsyncEnumerable<T> source, int n)
        {
            this.source = source;
            this.n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new SkipLastEnumerable(source.GetAsyncEnumerator(), n);
        }

        internal sealed class SkipLastEnumerable : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            int size;

            ArrayQueue<T> queue;

            T current;

            public T Current => current;

            public SkipLastEnumerable(IAsyncEnumerator<T> source, int n)
            {
                this.source = source;
                this.size = n;
                this.queue = new ArrayQueue<T>(16);
            }

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                while (size != 0)
                {
                    if (await source.MoveNextAsync())
                    {
                        queue.Enqueue(source.Current);
                        size--;
                    }
                    else
                    {
                        size = 0;
                        return false;
                    }
                }

                if (await source.MoveNextAsync())
                {
                    queue.Dequeue(out current);
                    queue.Enqueue(source.Current);
                    return true;
                }
                return false;
            }
        }
    }
}
