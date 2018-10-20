using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ZipArray<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T>[] sources;

        readonly Func<T[], R> zipper;

        public ZipArray(IAsyncEnumerable<T>[] sources, Func<T[], R> zipper)
        {
            this.sources = sources;
            this.zipper = zipper;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            var enumerators = new IAsyncEnumerator<T>[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                enumerators[i] = sources[i].GetAsyncEnumerator();
            }
            return new ZipArrayEnumerator(enumerators, zipper);
        }

        internal sealed class ZipArrayEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T>[] enumerators;

            readonly Func<T[], R> zipper;

            readonly ValueTask<bool>[] tasks;

            R current;

            bool done;

            public ZipArrayEnumerator(IAsyncEnumerator<T>[] enumerators, Func<T[], R> zipper)
            {
                this.enumerators = enumerators;
                this.zipper = zipper;
                this.tasks = new ValueTask<bool>[enumerators.Length];
            }

            public R Current => current;

            public async ValueTask DisposeAsync()
            {
                foreach (var en in enumerators)
                {
                    await en.DisposeAsync().ConfigureAwait(false);
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (done)
                {
                    return false;
                }

                current = default;

                var values = new T[enumerators.Length];

                for (var i = 0; i < enumerators.Length; i++)
                {
                    tasks[i] = enumerators[i].MoveNextAsync();
                }

                var fullRow = true;
                var errors = default(Exception);

                for (var i = 0; i < tasks.Length; i++)
                {
                    try
                    {
                        if (await tasks[i].ConfigureAwait(false))
                        {
                            values[i] = enumerators[i].Current;
                        }
                        else
                        {
                            fullRow = false;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (errors == null)
                        {
                            errors = ex;
                        }
                        else
                        {
                            errors = new AggregateException(errors, ex);
                        }
                    }
                }

                if (!fullRow)
                {
                    done = true;
                    if (errors != null)
                    {
                        throw errors;
                    }

                    return false;
                }

                current = zipper(values);
                return true;
            }
        }
    }
}
