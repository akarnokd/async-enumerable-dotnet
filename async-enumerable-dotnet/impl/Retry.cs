using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Retry<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly long n;

        readonly Func<long, Exception, bool> condition;

        public Retry(IAsyncEnumerable<T> source, long n, Func<long, Exception, bool> condition)
        {
            this.source = source;
            this.n = n;
            this.condition = condition;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new RetryEnumerator(source, source.GetAsyncEnumerator(), n, condition);
        }

        internal sealed class RetryEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerable<T> source;

            IAsyncEnumerator<T> current;

            long remaining;

            Func<long, Exception, bool> condition;

            public T Current => current.Current;

            long index;

            public RetryEnumerator(IAsyncEnumerable<T> source, IAsyncEnumerator<T> current, long remaining, Func<long, Exception, bool> condition)
            {
                this.source = source;
                this.current = current;
                this.remaining = remaining;
                this.condition = condition;
            }

            public ValueTask DisposeAsync()
            {
                return current.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    try
                    {
                        return await current.MoveNextAsync();
                    }
                    catch (Exception ex)
                    {
                        var n = remaining - 1;
                        if (n <= 0)
                        {
                            throw ex;
                        }

                        if (condition(index++, ex))
                        {
                            remaining = n;

                            await current.DisposeAsync();

                            current = source.GetAsyncEnumerator();
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }
        }
    }
}
