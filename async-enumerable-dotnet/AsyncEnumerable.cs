using System;
using System.Threading.Tasks;
using async_enumerable_dotnet.impl;

namespace async_enumerable_dotnet
{
    public static class AsyncEnumerable
    {
        public static IAsyncEnumerable<R> Zip<T, R>(Func<T[], R> zipper, params IAsyncEnumerable<T>[] sources)
        {
            return new ZipArray<T, R>(sources, zipper);
        }

        public static IAsyncEnumerable<T> FromArray<T>(params T[] values)
        {
            return new FromArray<T>(values);
        }

        public static IAsyncEnumerable<T> Timeout<T>(this IAsyncEnumerable<T> source, TimeSpan timeout)
        {
            return new Timeout<T>(source, timeout);
        }

        public static IAsyncEnumerable<long> Timer(TimeSpan delay)
        {
            return new Timer(delay);
        }

        public static IAsyncEnumerable<T> DoOnDispose<T>(this IAsyncEnumerable<T> source, Action handler)
        {
            return new DoOnDispose<T>(source, handler);
        }

        public static IAsyncEnumerable<T> DoOnDisposeAsync<T>(this IAsyncEnumerable<T> source, Func<ValueTask> handler)
        {
            return new DoOnDisposeAsync<T>(source, handler);
        }
    }
}
