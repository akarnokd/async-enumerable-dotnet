using System;
using System.Collections.Generic;
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

        public static IAsyncEnumerable<R> FlatMap<T, R>(this IAsyncEnumerable<T> source, Func<T, IAsyncEnumerable<R>> mapper, int maxConcurrency = int.MaxValue, int prefetch = 32)
        {
            return new FlatMap<T, R>(source, mapper, maxConcurrency, prefetch);
        }

        public static IAsyncEnumerable<R> Map<T, R>(this IAsyncEnumerable<T> source, Func<T, R> mapper)
        {
            return new Map<T, R>(source, mapper);
        }

        public static IAsyncEnumerable<T> FromObservable<T>(IObservable<T> source)
        {
            return new FromObservable<T>(source);
        }

        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IObservable<T> source)
        {
            return new FromObservable<T>(source);
        }

        public static IObservable<T> ToObservable<T>(this IAsyncEnumerable<T> source)
        {
            return new ToObservable<T>(source);
        }

        public static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            return new Filter<T>(source, predicate);
        }

        public static IAsyncEnumerable<T> FromEnumerable<T>(IEnumerable<T> source)
        {
            return new FromEnumerable<T>(source);
        }

        public static IAsyncEnumerable<T> Just<T>(T item)
        {
            return new Just<T>(item);
        }

        public static IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> source, long n)
        {
            return new Skip<T>(source, n);
        }
    }
}
