using System;
using System.Threading;
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
            return new impl.Timer(delay);
        }

        public static IAsyncEnumerable<long> Timer(TimeSpan delay, CancellationToken token)
        {
            return new TimerCancellable(delay, token);
        }

        public static IAsyncEnumerable<T> DoOnDispose<T>(this IAsyncEnumerable<T> source, Action handler)
        {
            return new DoOnDispose<T>(source, handler);
        }

        public static IAsyncEnumerable<T> DoOnDisposeAsync<T>(this IAsyncEnumerable<T> source, Func<ValueTask> handler)
        {
            return new DoOnDisposeAsync<T>(source, handler);
        }

        public static IAsyncEnumerable<T> Error<T>(Exception ex)
        {
            return new Error<T>(ex);
        }

        public static IAsyncEnumerable<T> Using<T, R>(Func<R> resourceProvider, Func<R, IAsyncEnumerable<T>> sourceProvider, Action<R> resourceCleanup)
        {
            return new Using<T, R>(resourceProvider, sourceProvider, resourceCleanup);
        }

        public static IAsyncEnumerable<T> FromTask<T>(Func<Task<T>> func)
        {
            return new FromTaskFunc<T>(func);
        }

        public static IAsyncEnumerable<T> Create<T>(Func<IAsyncEmitter<T>, Task> handler)
        {
            return new CreateEmitter<T>(handler);
        }

        public static IAsyncEnumerable<T> Defer<T>(Func<IAsyncEnumerable<T>> func)
        {
            return new Defer<T>(func);
        }

        public static IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, long n)
        {
            return new Take<T>(source, n);
        }

        public static IAsyncEnumerable<int> Range(int start, int count)
        {
            return new Range(start, start + count);
        }
    }
}
