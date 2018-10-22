using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Repeat<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly long n;

        readonly Func<long, bool> condition;

        public Repeat(IAsyncEnumerable<T> source, long n, Func<long, bool> condition)
        {
            this.source = source;
            this.n = n;
            this.condition = condition;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new RepeatEnumerator(source, condition, source.GetAsyncEnumerator(), n);
        }

        internal sealed class RepeatEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerable<T> source;

            readonly Func<long, bool> condition;

            IAsyncEnumerator<T> current;

            long remaining;

            long index;

            public T Current => current.Current;

            public RepeatEnumerator(IAsyncEnumerable<T> source, Func<long, bool> condition, IAsyncEnumerator<T> current, long remaining)
            {
                this.source = source;
                this.condition = condition;
                this.current = current;
                this.remaining = remaining;
            }

            public ValueTask DisposeAsync()
            {
                return current.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    if (await current.MoveNextAsync())
                    {
                        return true;
                    }

                    var n = remaining - 1;

                    if (n <= 0)
                    {
                        return false;
                    }

                    if (condition(index++))
                    {
                        await current.DisposeAsync();

                        current = source.GetAsyncEnumerator();

                        remaining = n;
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
