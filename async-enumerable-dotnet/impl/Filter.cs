using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Filter<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, bool> predicate;

        public Filter(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FilterEnumerator(source.GetAsyncEnumerator(), predicate);
        }

        internal sealed class FilterEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, bool> predicate;

            public FilterEnumerator(IAsyncEnumerator<T> source, Func<T, bool> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }

            public T Current => source.Current;

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    if (await source.MoveNextAsync())
                    {
                        if (predicate(source.Current))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }

    internal sealed class FilterAsync<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, Task<bool>> predicate;

        public FilterAsync(IAsyncEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FilterAsyncEnumerator(source.GetAsyncEnumerator(), predicate);
        }

        internal sealed class FilterAsyncEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, Task<bool>> predicate;

            public FilterAsyncEnumerator(IAsyncEnumerator<T> source, Func<T, Task<bool>> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }

            public T Current => source.Current;

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (await source.MoveNextAsync())
                    {
                        if (await predicate(source.Current))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}
