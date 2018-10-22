using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class TakeWhile<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, bool> predicate;

        public TakeWhile(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TakeWhileEnumerator(source.GetAsyncEnumerator(), predicate);
        }

        internal sealed class TakeWhileEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, bool> predicate;

            public T Current => source.Current;

            public TakeWhileEnumerator(IAsyncEnumerator<T> source, Func<T, bool> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await source.MoveNextAsync())
                {
                    return predicate(source.Current);
                }
                return false;
            }
        }
    }
}
