using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Map<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, R> mapper;

        public Map(IAsyncEnumerable<T> source, Func<T, R> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            return new MapEnumerator(source.GetAsyncEnumerator(), mapper);
        }

        internal sealed class MapEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, R> mapper;

            public MapEnumerator(IAsyncEnumerator<T> source, Func<T, R> mapper)
            {
                this.source = source;
                this.mapper = mapper;
            }

            public R Current => current;

            R current;

            public ValueTask DisposeAsync()
            {
                current = default;
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await source.MoveNextAsync())
                {
                    current = mapper(source.Current);
                    return true;
                }
                return false;
            }
        }
    }
}
