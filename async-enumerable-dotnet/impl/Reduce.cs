using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Reduce<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, T, T> reducer;

        public Reduce(IAsyncEnumerable<T> source, Func<T, T, T> reducer)
        {
            this.source = source;
            this.reducer = reducer;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ReduceEnumerator(source.GetAsyncEnumerator(), reducer);
        }

        internal sealed class ReduceEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, T, T> reducer;

            public T Current => current;

            bool once;

            T current;

            public ReduceEnumerator(IAsyncEnumerator<T> source, Func<T, T, T> reducer)
            {
                this.source = source;
                this.reducer = reducer;
            }

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    current = default;
                    return false;
                }
                once = true;

                var first = true;
                var accumulator = default(T);

                while (await source.MoveNextAsync())
                {
                    if (first)
                    {
                        accumulator = source.Current;
                        first = false;
                    }
                    else
                    {
                        accumulator = reducer(accumulator, source.Current);
                    }
                }

                if (first)
                {
                    return false;
                }
                current = accumulator;
                return true;
            }
        }
    }

    internal sealed class ReduceSeed<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<R> initialSupplier;

        readonly Func<R, T, R> reducer;

        public ReduceSeed(IAsyncEnumerable<T> source, Func<R> initialSupplier, Func<R, T, R> reducer)
        {
            this.source = source;
            this.initialSupplier = initialSupplier;
            this.reducer = reducer;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            var initial = default(R);
            try
            {
                initial = initialSupplier();
            }
            catch (Exception ex)
            {
                return new Error<R>.ErrorEnumerator(ex);
            }
            return new ReduceEnumerator(source.GetAsyncEnumerator(), initial, reducer);
        }

        internal sealed class ReduceEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<R, T, R> reducer;

            public R Current => accumulator;

            bool once;

            R accumulator;

            public ReduceEnumerator(IAsyncEnumerator<T> source, R initial, Func<R, T, R> reducer)
            {
                this.source = source;
                this.accumulator = initial;
                this.reducer = reducer;
            }

            public ValueTask DisposeAsync()
            {
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    accumulator = default;
                    return false;
                }
                once = true;

                while (await source.MoveNextAsync())
                {
                    accumulator = reducer(accumulator, source.Current);
                }

                return true;
            }
        }
    }
}
