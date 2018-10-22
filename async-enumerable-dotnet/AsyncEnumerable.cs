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

        public static IAsyncEnumerable<T> FromTask<T>(Task<T> task)
        {
            return new FromTask<T>(task);
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

        public static IAsyncEnumerable<R> Map<T, R>(this IAsyncEnumerable<T> source, Func<T, Task<R>> mapper)
        {
            return new MapAsync<T, R>(source, mapper);
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

        public static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            return new FilterAsync<T>(source, predicate);
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

        public static IAsyncEnumerable<T> Empty<T>()
        {
            return impl.Empty<T>.Instance;
        }

        public static IAsyncEnumerable<T> Never<T>()
        {
            return impl.Never<T>.Instance;
        }

        public static ValueTask ForEach<T>(this IAsyncEnumerable<T> source, Action<T> onNext = null, Action<Exception> onError = null, Action onComplete = null)
        {
            return impl.ForEach.ForEachAction<T>(source, onNext, onError, onComplete);
        }

        public static ValueTask<T> FirstTask<T>(this IAsyncEnumerable<T> source)
        {
            return impl.FirstLastSingleTask.First(source, default, false);
        }

        public static ValueTask<T> FirstTask<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            return impl.FirstLastSingleTask.First(source, defaultItem, true);
        }

        public static ValueTask<T> LastTask<T>(this IAsyncEnumerable<T> source)
        {
            return impl.FirstLastSingleTask.Last(source, default, false);
        }

        public static ValueTask<T> LastTask<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            return impl.FirstLastSingleTask.Last(source, defaultItem, true);
        }

        public static ValueTask<T> SingleTask<T>(this IAsyncEnumerable<T> source)
        {
            return impl.FirstLastSingleTask.Single(source, default, false);
        }

        public static ValueTask<T> SingleTask<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            return impl.FirstLastSingleTask.Single(source, defaultItem, true);
        }

        public static IAsyncEnumerable<T> Reduce<T>(this IAsyncEnumerable<T> source, Func<T, T, T> reducer)
        {
            return new Reduce<T>(source, reducer);
        }

        public static IAsyncEnumerable<R> Reduce<T, R>(this IAsyncEnumerable<T> source, Func<R> initialSupplier, Func<R, T, R> reducer)
        {
            return new ReduceSeed<T, R>(source, initialSupplier, reducer);
        }

        public static IAsyncEnumerable<C> Collect<T, C>(this IAsyncEnumerable<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            return new Collect<T, C>(source, collectionSupplier, collector);
        }

        public static IAsyncEnumerable<R> ConcatMap<T, R>(this IAsyncEnumerable<T> source, Func<T, IAsyncEnumerable<R>> mapper)
        {
            return new ConcatMap<T, R>(source, mapper);
        }

        public static IAsyncEnumerable<R> ConcatMap<T, R>(this IAsyncEnumerable<T> source, Func<T, IEnumerable<R>> mapper)
        {
            return new ConcatMapEnumerable<T, R>(source, mapper);
        }

        public static IAsyncEnumerable<T> DoOnNext<T>(this IAsyncEnumerable<T> source, Action<T> handler)
        {
            return new DoOnNext<T>(source, handler);
        }

        public static IAsyncEnumerable<T> DoOnNext<T>(this IAsyncEnumerable<T> source, Func<T, Task> handler)
        {
            return new DoOnNextAsync<T>(source, handler);
        }

        public static IEnumerable<T> ToEnumerable<T>(this IAsyncEnumerable<T> source)
        {
            return new ToEnumerable<T>(source);
        }

        public static IAsyncEnumerable<T> OnErrorResumeNext<T>(this IAsyncEnumerable<T> source, Func<Exception, IAsyncEnumerable<T>> handler)
        {
            return new OnErrorResumeNext<T>(source, handler);
        }

        public static IAsyncEnumerable<T> Concat<T>(params IAsyncEnumerable<T>[] sources)
        {
            return new Concat<T>(sources);
        }

        public static IAsyncEnumerable<T> Concat<T>(IEnumerable<IAsyncEnumerable<T>> sources)
        {
            return new ConcatEnumerable<T>(sources);
        }

        public static IAsyncEnumerable<T> ConcatWith<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> other)
        {
            return Concat(source, other);
        }

        public static IAsyncEnumerable<T> TakeUntil<T, U>(this IAsyncEnumerable<T> source, IAsyncEnumerable<U> other)
        {
            return new TakeUntil<T, U>(source, other);
        }

        public static IAsyncEnumerable<long> Interval(TimeSpan period)
        {
            return new Interval(0, long.MinValue, period, period);
        }

        public static IAsyncEnumerable<long> Interval(TimeSpan initialDelay, TimeSpan period)
        {
            return new Interval(0, long.MinValue, initialDelay, period);
        }

        public static IAsyncEnumerable<long> IntervalRange(long start, long count, TimeSpan period)
        {
            return new Interval(start, start + count, period, period);
        }

        public static IAsyncEnumerable<long> Interval(long start, long count, TimeSpan initialDelay, TimeSpan period)
        {
            return new Interval(start, start + count, initialDelay, period);
        }
    }
}
