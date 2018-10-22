using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Scan<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, T, T> scanner;

        public Scan(IAsyncEnumerable<T> source, Func<T, T, T> scanner)
        {
            this.source = source;
            this.scanner = scanner;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ScanEnumerator(source.GetAsyncEnumerator(), scanner);
        }

        internal sealed class ScanEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, T, T> scanner;

            bool once;

            T current;

            public T Current => current;

            public ScanEnumerator(IAsyncEnumerator<T> source, Func<T, T, T> scanner)
            {
                this.source = source;
                this.scanner = scanner;
            }

            public ValueTask DisposeAsync()
            {
                current = default;
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (!once)
                {
                    once = true;
                    if (await source.MoveNextAsync())
                    {
                        current = source.Current;
                        return true;
                    }
                    return false;
                }
                if (await source.MoveNextAsync())
                {
                    current = scanner(current, source.Current);
                    return true;
                }
                return false;
            }
        }
    }

    internal sealed class ScanSeed<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<R> initialSupplier;

        readonly Func<R, T, R> scanner;

        public ScanSeed(IAsyncEnumerable<T> source, Func<R> initialSupplier, Func<R, T, R> scanner)
        {
            this.source = source;
            this.initialSupplier = initialSupplier;
            this.scanner = scanner;
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

            return new ScanSeedEnumerator(source.GetAsyncEnumerator(), scanner, initial);
        }

        internal sealed class ScanSeedEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<R, T, R> scanner;

            R current;

            public R Current => current;

            bool once;

            public ScanSeedEnumerator(IAsyncEnumerator<T> source, Func<R, T, R> scanner, R current)
            {
                this.source = source;
                this.scanner = scanner;
                this.current = current;
            }

            public ValueTask DisposeAsync()
            {
                current = default;
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (!once)
                {
                    once = true;
                    return true;
                }

                if (await source.MoveNextAsync())
                {
                    current = scanner(current, source.Current);
                    return true;
                }
                return false;
            }
        }
    }
}
