using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class BufferExact<T, C> : IAsyncEnumerable<C> where C : ICollection<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly int n;

        readonly Func<C> bufferSupplier;

        public BufferExact(IAsyncEnumerable<T> source, int n, Func<C> bufferSupplier)
        {
            this.source = source;
            this.n = n;
            this.bufferSupplier = bufferSupplier;
        }

        public IAsyncEnumerator<C> GetAsyncEnumerator()
        {
            return new BufferExactEnumerator(source.GetAsyncEnumerator(), n, bufferSupplier);
        }

        internal sealed class BufferExactEnumerator : IAsyncEnumerator<C>
        {

            readonly IAsyncEnumerator<T> source;

            readonly int n;

            readonly Func<C> bufferSupplier;

            C buffer;

            public C Current => buffer;

            int count;

            bool done;

            public BufferExactEnumerator(IAsyncEnumerator<T> source, int n, Func<C> bufferSupplier)
            {
                this.source = source;
                this.n = n;
                this.bufferSupplier = bufferSupplier;
            }

            public ValueTask DisposeAsync()
            {
                buffer = default;
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (done)
                {
                    return false;
                }
                var buf = bufferSupplier();

                while (count != n)
                {
                    if (await source.MoveNextAsync())
                    {
                        buf.Add(source.Current);
                        count++;
                    }
                    else
                    {
                        done = true;
                        break;
                    }
                }

                if (count == 0)
                {
                    return false;
                }

                count = 0;
                buffer = buf;
                return true;
            }
        }
    }

    internal sealed class BufferSkip<T, C> : IAsyncEnumerable<C> where C : ICollection<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly int size;

        readonly int skip;

        readonly Func<C> bufferSupplier;

        public BufferSkip(IAsyncEnumerable<T> source, int size, int skip, Func<C> bufferSupplier)
        {
            this.source = source;
            this.size = size;
            this.skip = skip;
            this.bufferSupplier = bufferSupplier;
        }

        public IAsyncEnumerator<C> GetAsyncEnumerator()
        {
            return new BufferSkipEnumerator(source.GetAsyncEnumerator(), size, skip, bufferSupplier);
        }

        internal sealed class BufferSkipEnumerator : IAsyncEnumerator<C>
        {

            readonly IAsyncEnumerator<T> source;

            readonly int size;

            readonly int skip;

            readonly Func<C> bufferSupplier;

            C buffer;

            public C Current => buffer;

            int count;

            bool once;

            public BufferSkipEnumerator(IAsyncEnumerator<T> source, int size, int skip, Func<C> bufferSupplier)
            {
                this.source = source;
                this.size = size;
                this.skip = skip;
                this.bufferSupplier = bufferSupplier;
            }

            public ValueTask DisposeAsync()
            {
                buffer = default;
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    while (count++ != skip)
                    {
                        if (!await source.MoveNextAsync())
                        {
                            return false;
                        }
                    }
                }

                once = true;

                var buf = bufferSupplier();
                count = 0;
                while (count != size)
                {
                    if (await source.MoveNextAsync())
                    {
                        buf.Add(source.Current);
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (count == 0)
                {
                    return false;
                }

                buffer = buf;
                return true;
            }
        }
    }

    internal sealed class BufferOverlap<T, C> : IAsyncEnumerable<C> where C : ICollection<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly int size;

        readonly int skip;

        readonly Func<C> bufferSupplier;

        public BufferOverlap(IAsyncEnumerable<T> source, int size, int skip, Func<C> bufferSupplier)
        {
            this.source = source;
            this.size = size;
            this.skip = skip;
            this.bufferSupplier = bufferSupplier;
        }

        public IAsyncEnumerator<C> GetAsyncEnumerator()
        {
            return new BufferOverlapEnumerator(source.GetAsyncEnumerator(), size, skip, bufferSupplier);
        }

        internal sealed class BufferOverlapEnumerator : IAsyncEnumerator<C>
        {

            readonly IAsyncEnumerator<T> source;

            readonly int size;

            readonly int skip;

            readonly Func<C> bufferSupplier;

            C buffer;

            public C Current => buffer;

            int index;

            int count;

            bool done;

            ArrayQueue<C> buffers;

            public BufferOverlapEnumerator(IAsyncEnumerator<T> source, int size, int skip, Func<C> bufferSupplier)
            {
                this.source = source;
                this.size = size;
                this.skip = skip;
                this.bufferSupplier = bufferSupplier;
                this.buffers = new ArrayQueue<C>(16);
            }

            public ValueTask DisposeAsync()
            {
                buffer = default;
                buffers.Release();
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (done)
                    {
                        if (buffers.Dequeue(out buffer))
                        {
                            return true;
                        }
                        return false;
                    }


                    if (await source.MoveNextAsync())
                    {
                        if (index == 0)
                        {
                            buffers.Enqueue(bufferSupplier());
                        }

                        index++;
                        count++;
                        buffers.ForEach(source.Current, (b, v) => b.Add(v));
                    }
                    else
                    {
                        done = true;
                        continue;
                    }

                    if (index == skip)
                    {
                        index = 0;
                    }
                    if (count == size)
                    {
                        count -= skip;
                        buffers.Dequeue(out buffer);
                        return true;
                    }
                }
            }
        }
    }
}
