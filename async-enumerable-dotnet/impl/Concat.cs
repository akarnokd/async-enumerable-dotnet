using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Concat<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T>[] sources;

        public Concat(IAsyncEnumerable<T>[] sources)
        {
            this.sources = sources;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ConcatEnumerator(sources);
        }

        internal sealed class ConcatEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerable<T>[] sources;

            int index;

            IAsyncEnumerator<T> current;

            public ConcatEnumerator(IAsyncEnumerable<T>[] sources)
            {
                this.sources = sources;
            }

            public T Current => current.Current;

            public ValueTask DisposeAsync()
            {
                if (current != null)
                {
                    return current.DisposeAsync();
                }
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (current == null)
                    {
                        var idx = index;
                        if (idx == sources.Length)
                        {
                            return false;
                        }

                        index = idx + 1;
                        current = sources[idx].GetAsyncEnumerator();
                    }

                    if (await current.MoveNextAsync())
                    {
                        return true;
                    }

                    await current.DisposeAsync();
                    current = null;
                }
            }
        }
    }

    internal sealed class ConcatEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly IEnumerable<IAsyncEnumerable<T>> sources;

        public ConcatEnumerable(IEnumerable<IAsyncEnumerable<T>> sources)
        {
            this.sources = sources;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ConcatEnumerator(sources.GetEnumerator());
        }

        internal sealed class ConcatEnumerator : IAsyncEnumerator<T>
        {
            readonly IEnumerator<IAsyncEnumerable<T>> sources;

            IAsyncEnumerator<T> current;

            public ConcatEnumerator(IEnumerator<IAsyncEnumerable<T>> sources)
            {
                this.sources = sources;
            }

            public T Current => current.Current;

            public ValueTask DisposeAsync()
            {
                sources.Dispose();
                if (current != null)
                {
                    return current.DisposeAsync();
                }
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (current == null)
                    {
                        if (sources.MoveNext())
                        {
                            current = sources.Current.GetAsyncEnumerator();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (await current.MoveNextAsync())
                    {
                        return true;
                    }

                    await current.DisposeAsync();
                    current = null;
                }
            }
        }
    }
}
