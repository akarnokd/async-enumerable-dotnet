using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class OnErrorResumeNext<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<Exception, IAsyncEnumerable<T>> handler;

        public OnErrorResumeNext(IAsyncEnumerable<T> source, Func<Exception, IAsyncEnumerable<T>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new OnErrorResumeNextEnumerator(source.GetAsyncEnumerator(), handler);
        }

        internal sealed class OnErrorResumeNextEnumerator : IAsyncEnumerator<T>
        {
            IAsyncEnumerator<T> source;

            Func<Exception, IAsyncEnumerable<T>> handler;

            public OnErrorResumeNextEnumerator(IAsyncEnumerator<T> source, Func<Exception, IAsyncEnumerable<T>> handler)
            {
                this.source = source;
                this.handler = handler;
            }

            public T Current => source.Current;

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (handler == null)
                {
                    return await source.MoveNextAsync();
                }
                else
                {
                    try
                    {
                        return await source.MoveNextAsync();
                    }
                    catch (Exception ex)
                    {
                        var en = default(IAsyncEnumerator<T>);

                        try
                        {
                            en = handler(ex).GetAsyncEnumerator();
                        }
                        catch (Exception exc)
                        {
                            throw new AggregateException(ex, exc);
                        }

                        handler = null;
                        source = en;

                        return await en.MoveNextAsync();
                    }
                }
            }
        }
    }
}
