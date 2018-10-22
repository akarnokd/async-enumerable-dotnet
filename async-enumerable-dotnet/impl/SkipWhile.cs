using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SkipWhile<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, bool> predicate;

        public SkipWhile(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new SkipWhileEnumerator(source.GetAsyncEnumerator(), predicate);
        }

        internal sealed class SkipWhileEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, bool> predicate;

            public T Current => source.Current;

            bool once;

            public SkipWhileEnumerator(IAsyncEnumerator<T> source, Func<T, bool> predicate)
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
                if (once)
                {
                    return await source.MoveNextAsync();
                }
                else
                {
                    for (; ;)
                    {
                        if (await source.MoveNextAsync())
                        {
                            if (!predicate(source.Current))
                            {
                                once = true;
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
}
