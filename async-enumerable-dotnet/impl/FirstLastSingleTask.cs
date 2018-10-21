using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal static class FirstLastSingleTask
    {
        internal static async ValueTask<T> First<T>(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            var enumerator = source.GetAsyncEnumerator();
            try
            {
                if (await enumerator.MoveNextAsync())
                {
                    return enumerator.Current;
                }
                else
                {
                    if (hasDefault)
                    {
                        return defaultItem;
                    }
                    throw new IndexOutOfRangeException("The source async sequence is empty");
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        internal static async ValueTask<T> Last<T>(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            var hasLast = false;
            var last = default(T);

            var enumerator = source.GetAsyncEnumerator();
            try
            {

                while (await enumerator.MoveNextAsync())
                {
                    last = enumerator.Current;
                    hasLast = true;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            if (hasLast)
            {
                return last;
            }
            if (hasDefault)
            {
                return defaultItem;
            }
            throw new IndexOutOfRangeException("The source async sequence is empty");
        }

        internal static async ValueTask<T> Single<T>(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            var enumerator = source.GetAsyncEnumerator();
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    if (hasDefault)
                    {
                        return defaultItem;
                    }

                    throw new IndexOutOfRangeException("The source async sequence is empty");
                }

                var value = enumerator.Current;

                if (await enumerator.MoveNextAsync())
                {
                    throw new IndexOutOfRangeException("The source async sequence has more than one value");
                }

                return value;
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }
    }
}
