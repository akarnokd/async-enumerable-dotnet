using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ConcatMap<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, IAsyncEnumerable<R>> mapper;

        public ConcatMap(IAsyncEnumerable<T> source, Func<T, IAsyncEnumerable<R>> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            return new ConcatMapEnumerator(source.GetAsyncEnumerator(), mapper);
        }

        internal sealed class ConcatMapEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, IAsyncEnumerable<R>> mapper;

            public R Current => current;

            R current;

            IAsyncEnumerator<R> inner;

            public ConcatMapEnumerator(IAsyncEnumerator<T> source, Func<T, IAsyncEnumerable<R>> mapper)
            {
                this.source = source;
                this.mapper = mapper;
            }

            public async ValueTask DisposeAsync()
            {
                if (inner != null)
                {
                    await inner.DisposeAsync();
                }
                await source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                current = default;
                for (; ;)
                {
                    if (inner == null)
                    {
                        if (await source.MoveNextAsync())
                        {
                            inner = mapper(source.Current).GetAsyncEnumerator();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (await inner.MoveNextAsync())
                    {
                        current = inner.Current;
                        return true;
                    }
                    else
                    {
                        await inner.DisposeAsync();
                        inner = null;
                    }
                }
            }
        }
    }

    internal sealed class ConcatMapEnumerable<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, IEnumerable<R>> mapper;

        public ConcatMapEnumerable(IAsyncEnumerable<T> source, Func<T, IEnumerable<R>> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            return new ConcatMapEnumerator(source.GetAsyncEnumerator(), mapper);
        }

        internal sealed class ConcatMapEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, IEnumerable<R>> mapper;

            public R Current => current;

            R current;

            IEnumerator<R> inner;

            public ConcatMapEnumerator(IAsyncEnumerator<T> source, Func<T, IEnumerable<R>> mapper)
            {
                this.source = source;
                this.mapper = mapper;
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                    inner?.Dispose();
                }
                finally
                {
                    await source.DisposeAsync();
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                current = default;
                for (; ; )
                {
                    if (inner == null)
                    {
                        if (await source.MoveNextAsync())
                        {
                            inner = mapper(source.Current).GetEnumerator();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (inner.MoveNext())
                    {
                        current = inner.Current;
                        return true;
                    }
                    else
                    {
                        inner.Dispose();
                        inner = null;
                    }
                }
            }
        }
    }
}
