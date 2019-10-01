// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using async_enumerable_dotnet.impl;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// Factory and extension methods for working with <see cref="IAsyncEnumerable{T}"/>s.
    /// </summary>
    public static class AsyncEnumerable
    {
        #region - ValidationHelper -
        /// <summary>
        /// Checks if the argument is null and throws.
        /// </summary>
        /// <typeparam name="TValue">The class type.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <param name="argumentName">The argument name for the ArgumentNullException</param>
        /// <exception cref="ArgumentNullException">If <paramref name="value"/> is null.</exception>
        /// <returns>The value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        internal static void RequireNonNull<TValue>(TValue value, string argumentName) where TValue : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Check if the value is positive.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="argumentName">The argument name for the ArgumentNullException</param>
        /// <exception cref="ArgumentNullException">If <paramref name="value"/> is null.</exception>
        /// <returns>The value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RequirePositive(int value, string argumentName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, value, "must be positive");
            }
        }

        /// <summary>
        /// Check if the value is positive.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="argumentName">The argument name for the ArgumentNullException</param>
        /// <exception cref="ArgumentNullException">If <paramref name="value"/> is null.</exception>
        /// <returns>The value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RequirePositive(long value, string argumentName)
        {
            if (value <= 0L)
            {
                throw new ArgumentOutOfRangeException(argumentName, value, "must be positive");
            }
        }

        #endregion - ValidationHelper -

        /// <summary>
        /// Combine a row of the next elements from each async sequence via a zipper function.
        /// </summary>
        /// <typeparam name="TSource">The common element type of the sources.</typeparam>
        /// <typeparam name="TResult">The resulting element type.</typeparam>
        /// <param name="zipper">The function that receives the next items from the sources in
        /// an array and should return the value to be passed forward.</param>
        /// <param name="sources">The params array of async sequences.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> Zip<TSource, TResult>(Func<TSource[], TResult> zipper, params IAsyncEnumerable<TSource>[] sources)
        {
            RequireNonNull(sources, nameof(sources));
            RequireNonNull(zipper, nameof(zipper));
            return new ZipArray<TSource, TResult>(sources, zipper);
        }

        /// <summary>
        /// Wraps an array of items into an async sequence that emits its elements.
        /// </summary>
        /// <typeparam name="TValue">The element type.</typeparam>
        /// <param name="values">The params array of values to emit.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TValue> FromArray<TValue>(params TValue[] values)
        {
            RequireNonNull(values, nameof(values));
            return new FromArray<TValue>(values);
        }

        /// <summary>
        /// Signals a <see cref="TimeoutException"/> when more than the specified amount of
        /// time passes between items.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <param name="source">The source sequence to relay items of.</param>
        /// <param name="timeout">The timeout between items.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TSource> Timeout<TSource>(this IAsyncEnumerable<TSource> source, TimeSpan timeout)
        {
            RequireNonNull(source, nameof(source));
            return new Timeout<TSource>(source, timeout);
        }

        /// <summary>
        /// Signals a 0L and completes after the specified time delay.
        /// </summary>
        /// <param name="delay">The time delay before emitting 0L and completing.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<long> Timer(TimeSpan delay)
        {
            return new impl.Timer(delay);
        }

        /// <summary>
        /// Signals a 0L and completes after the specified time delay, allowing external
        /// cancellation via the given CancellationToken.
        /// </summary>
        /// <param name="delay">The time delay before emitting 0L and completing.</param>
        /// <param name="token">The token to cancel the timer.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        /// <remarks>
        /// Note that the <see cref="CancellationToken"/> is shared across all instantiations of the
        /// async sequence and thus it is recommended this Timer is created in a deferred manner,
        /// such as <see cref="AsyncEnumerable.Defer{T}"/>.
        /// </remarks>
        public static IAsyncEnumerable<long> Timer(TimeSpan delay, CancellationToken token)
        {
            return new TimerCancellable(delay, token);
        }

        /// <summary>
        /// Calls the specified synchronous handler when the async sequence is disposed via
        /// <see cref="IAsyncDisposable.DisposeAsync"/>.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <param name="source">The source sequence to relay items of.</param>
        /// <param name="handler">The handler called when the sequence is disposed.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TSource> DoOnDispose<TSource>(this IAsyncEnumerable<TSource> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));
            return new DoOnDispose<TSource>(source, handler);
        }

        /// <summary>
        /// Calls the specified asynchronous handler when the async sequence is disposed via
        /// <see cref="IAsyncDisposable.DisposeAsync"/>.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <param name="source">The source sequence to relay items of.</param>
        /// <param name="handler">The async handler called when the sequence is disposed.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TSource> DoOnDispose<TSource>(this IAsyncEnumerable<TSource> source, Func<ValueTask> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));
            return new DoOnDisposeAsync<TSource>(source, handler);
        }

        /// <summary>
        /// Signals the given Exception immediately.
        /// </summary>
        /// <typeparam name="TResult">The intended element type of the sequence.</typeparam>
        /// <param name="exception">The exception to signal immediately.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> Error<TResult>(Exception exception)
        {
            RequireNonNull(exception, nameof(exception));
            return new Error<TResult>(exception);
        }

        /// <summary>
        /// Creates a resource and an actual async sequence with the help of this resource
        /// to be relayed and cleaned up afterwards.
        /// </summary>
        /// <typeparam name="TSource">The element type of the async sequence.</typeparam>
        /// <typeparam name="TResource">The resource type.</typeparam>
        /// <param name="resourceProvider">The function that returns the resource to be used.</param>
        /// <param name="sourceProvider">The function that produces the actual async sequence for
        /// the generated resource.</param>
        /// <param name="resourceCleanup">The action to cleanup the resource after the generated
        /// async sequence terminated.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TSource> Using<TSource, TResource>(Func<TResource> resourceProvider, Func<TResource, IAsyncEnumerable<TSource>> sourceProvider, Action<TResource> resourceCleanup)
        {
            RequireNonNull(resourceProvider, nameof(resourceProvider));
            RequireNonNull(sourceProvider, nameof(sourceProvider));
            RequireNonNull(resourceCleanup, nameof(resourceCleanup));
            return new Using<TSource, TResource>(resourceProvider, sourceProvider, resourceCleanup);
        }

        /// <summary>
        /// Create a task and emit its result/error as an async sequence.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="func">The function that returns a task that will create an item.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> FromTask<TResult>(Func<Task<TResult>> func)
        {
            RequireNonNull(func, nameof(func));
            return new FromTaskFunc<TResult>(func);
        }

        /// <summary>
        /// Wrap an existing task and emit its result/error when it terminates as
        /// an async sequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="task">The task to wrap.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> FromTask<T>(Task<T> task)
        {
            RequireNonNull(task, nameof(task));
            return new FromTask<T>(task);
        }

        /// <summary>
        /// Creates an async sequence which emits items generated by the handler asynchronous function
        /// via the <see cref="IAsyncEmitter{T}"/> provided. The sequence ends when
        /// the task completes or fails.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="handler">The function that receives the emitter to be used for emitting items
        /// and should return a task which when terminates, the resulting async sequence terminates.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Create<T>(Func<IAsyncEmitter<T>, Task> handler)
        {
            RequireNonNull(handler, nameof(handler));
            return new CreateEmitter<T>(handler);
        }

        /// <summary>
        /// Defers the creation of the actual async sequence until the <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/> is called on the returned async sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequence.</typeparam>
        /// <param name="func">The function called when the async enumerator is requested.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Defer<T>(Func<IAsyncEnumerable<T>> func)
        {
            RequireNonNull(func, nameof(func));
            return new Defer<T>(func);
        }

        /// <summary>
        /// Relays at most the given number of items from the source async sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequence.</typeparam>
        /// <param name="source">The source async sequence to limit.</param>
        /// <param name="n">The number of items to let pass.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, long n)
        {
            RequireNonNull(source, nameof(source));
            return new Take<T>(source, n);
        }

        /// <summary>
        /// Generates a range of integer values as an async sequence.
        /// </summary>
        /// <param name="start">The starting value.</param>
        /// <param name="count">The number of items to generate.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<int> Range(int start, int count)
        {
            return new impl.Range(start, start + count);
        }

        /// <summary>
        /// Maps the items of the source async sequence into async sequences and then
        /// merges the elements of those inner sequences into a one sequence.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source async sequence.</typeparam>
        /// <typeparam name="TResult">The element type of the inner async sequences.</typeparam>
        /// <param name="source">The source that emits items to be mapped.</param>
        /// <param name="mapper">The function that takes an source item and should return
        /// an async sequence to be merged.</param>
        /// <param name="maxConcurrency">The maximum number of inner sequences to run at once.</param>
        /// <param name="prefetch">The number of items to prefetch from each inner async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> FlatMap<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper, int maxConcurrency = int.MaxValue, int prefetch = 32)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequirePositive(maxConcurrency, nameof(maxConcurrency));
            RequirePositive(prefetch, nameof(prefetch));
            return new FlatMap<TSource, TResult>(source, mapper, maxConcurrency, prefetch);
        }

        /// <summary>
        /// Transforms each source item into another item via a function.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source async sequence to transform.</param>
        /// <param name="mapper">The function that takes a source item and should return the result item
        /// to be emitted.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> Map<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            return new Map<TSource, TResult>(source, mapper);
        }

        /// <summary>
        /// Transforms each source item into another item via an asynchronous function.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source async sequence to transform.</param>
        /// <param name="mapper">The function that takes a source item and should return a task that produces
        /// the result item.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> Map<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, Task<TResult>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            return new MapAsync<TSource, TResult>(source, mapper);
        }

        /// <summary>
        /// Wraps an <see cref="IObservable{T}"/> and turns it into an async sequence that
        /// buffers all items until they are requested by the consumer of the async sequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source observable sequence to turn into an async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> FromObservable<T>(IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new FromObservable<T>(source);
        }

        /// <summary>
        /// Wraps an <see cref="IObservable{T}"/> and turns it into an async sequence that
        /// buffers all items until they are requested by the consumer of the async sequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source observable sequence to turn into an async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new FromObservable<T>(source);
        }

        /// <summary>
        /// Converts an async sequence into an <see cref="IObservable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source async sequence to turn into an observable sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IObservable<T> ToObservable<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new ToObservable<T>(source);
        }

        /// <summary>
        /// Filters out source items that don't pass the provided predicate.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to filter.</param>
        /// <param name="predicate">The function receiving the source item and should
        /// return true if that item can be passed along.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));
            return new Filter<T>(source, predicate);
        }

        /// <summary>
        /// Filters out source items that don't pass the provided asynchronous predicate.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to filter.</param>
        /// <param name="predicate">The function receiving the source item and should
        /// return a task that signals true if that item can be passed along.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));
            return new FilterAsync<T>(source, predicate);
        }

        /// <summary>
        /// Wraps an <see cref="IEnumerable{T}"/> sequence into an async sequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source sequence to turn into an async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> FromEnumerable<T>(IEnumerable<T> source)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            RequireNonNull(source, nameof(source));
            // ReSharper disable once PossibleMultipleEnumeration
            return new FromEnumerable<T>(source);
        }

        /// <summary>
        /// Creates an async sequence that emits the given preexisting value.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="item">The item to emit.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Just<T>(T item)
        {
            return new Just<T>(item);
        }

        /// <summary>
        /// Skips the first given number of items of the source async sequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="n">The number of items to skip.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> source, long n)
        {
            RequireNonNull(source, nameof(source));
            return new Skip<T>(source, n);
        }

        /// <summary>
        /// Returns a shared instance of an empty async sequence.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <returns>The shared empty IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Empty<T>()
        {
            return impl.Empty<T>.Instance;
        }

        /// <summary>
        /// Returns a shared instance of an async sequence that never produces any items or terminates.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <returns>The shared non-signaling IAsyncEnumerable instance.</returns>
        /// <remarks>
        /// Note that the async sequence API doesn't really support a never emitting source because
        /// such source never completes its MoveNextAsync and thus DisposeAsync can't be called.
        /// </remarks>
        public static IAsyncEnumerable<T> Never<T>()
        {
            return impl.Never<T>.Instance;
        }

        /// <summary>
        /// Consumes the source async sequence by calling the relevant callback action for the
        /// items and terminal signals.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="onNext">The action called for each item when they become available.</param>
        /// <param name="onError">The action called when the source async sequence fails.</param>
        /// <param name="onComplete">The action called when the source sequence completes normally.</param>
        /// <returns>The task that completes when the sequence terminates.</returns>
        public static ValueTask ForEach<T>(this IAsyncEnumerable<T> source, Action<T> onNext = null, Action<Exception> onError = null, Action onComplete = null)
        {
            RequireNonNull(source, nameof(source));
            return impl.ForEach.ForEachAction(source, onNext, onError, onComplete);
        }

        /// <summary>
        /// Returns a task that produces the first item of the source async sequence
        /// or an <see cref="IndexOutOfRangeException"/> if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the first item of.</param>
        /// <returns>The task that completes with the first item of the sequence or fails.</returns>
        public static ValueTask<T> FirstAsync<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return FirstLastSingleAsync.First(source, default, false);
        }

        /// <summary>
        /// Returns a task that produces the first item of the source async sequence
        /// or the given default item if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the first item of.</param>
        /// <param name="defaultItem">The item to return if the source is empty.</param>
        /// <returns>The task that completes with the first/default item of the sequence or fails.</returns>
        public static ValueTask<T> FirstAsync<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));
            return FirstLastSingleAsync.First(source, defaultItem, true);
        }

        /// <summary>
        /// Returns a task that produces the last item of the source async sequence
        /// or an <see cref="IndexOutOfRangeException"/> if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the last item of.</param>
        /// <returns>The task that completes with the last item of the sequence or fails.</returns>
        public static ValueTask<T> LastAsync<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return FirstLastSingleAsync.Last(source, default, false);
        }

        /// <summary>
        /// Returns a task that produces the last item of the source async sequence
        /// or the given default item if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the last item of.</param>
        /// <param name="defaultItem">The item to return if the source is empty.</param>
        /// <returns>The task that completes with the last/default item of the sequence or fails.</returns>
        public static ValueTask<T> LastAsync<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));
            return FirstLastSingleAsync.Last(source, defaultItem, true);
        }

        /// <summary>
        /// Returns a task that produces the only item of the source async sequence
        /// or an <see cref="IndexOutOfRangeException"/> if the source is empty or has more items.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the only item of.</param>
        /// <returns>The task that completes with the only item of the sequence or fails.</returns>
        public static ValueTask<T> SingleAsync<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return FirstLastSingleAsync.Single(source, default, false);
        }

        /// <summary>
        /// Returns a task that produces the only item of the source async sequence
        /// or the given default item if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the only item of.</param>
        /// <param name="defaultItem">The item to return if the source is empty.</param>
        /// <returns>The task that completes with the only/default item of the sequence or fails.</returns>
        public static ValueTask<T> SingleAsync<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));
            return FirstLastSingleAsync.Single(source, defaultItem, true);
        }

        /// <summary>
        /// Combines an accumulator and the next item through a function to produce
        /// a new accumulator of which the last accumulator value
        /// is the result item.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source to reduce into a single value.</param>
        /// <param name="reducer">The function that takes the previous accumulator value (or the first item), the current item and should produce the new accumulator value.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Reduce<T>(this IAsyncEnumerable<T> source, Func<T, T, T> reducer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(reducer, nameof(reducer));
            return new Reduce<T>(source, reducer);
        }

        /// <summary>
        /// Combines an accumulator and the next item through a function to produce
        /// a new accumulator of which the last accumulator value
        /// is the result item.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <typeparam name="TResult">The accumulator and result type.</typeparam>
        /// <param name="source">The source to reduce into a single value.</param>
        /// <param name="initialSupplier">The function returning the initial accumulator value.</param>
        /// <param name="reducer">The function that takes the previous accumulator value (or the first item), the current item and should produce the new accumulator value.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> Reduce<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TResult> initialSupplier, Func<TResult, TSource, TResult> reducer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(initialSupplier, nameof(initialSupplier));
            RequireNonNull(reducer, nameof(reducer));
            return new ReduceSeed<TSource, TResult>(source, initialSupplier, reducer);
        }

        /// <summary>
        /// Generates a collection and calls an action with
        /// the current item to be combined into it and then
        /// the collection is emitted as the final result.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source async sequence.</typeparam>
        /// <typeparam name="TCollection">The collection and result type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="collectionSupplier">The function that generates the collection.</param>
        /// <param name="collector">The action called with the collection and the current source item.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TCollection> Collect<TSource, TCollection>(this IAsyncEnumerable<TSource> source, Func<TCollection> collectionSupplier, Action<TCollection, TSource> collector)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(collectionSupplier, nameof(collectionSupplier));
            RequireNonNull(collector, nameof(collector));
            return new Collect<TSource, TCollection>(source, collectionSupplier, collector);
        }

        /// <summary>
        /// Maps the source items into inner async sequences and relays their items one after the other sequence.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source async sequence.</typeparam>
        /// <typeparam name="TResult">The element type of the inner sequences and the result items.</typeparam>
        /// <param name="source">The source async sequence to be mapped.</param>
        /// <param name="mapper">The function receiving the source item and should return an inner async sequence to relay elements of.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> ConcatMap<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            return new ConcatMap<TSource, TResult>(source, mapper);
        }

        /// <summary>
        /// Maps the source items into inner enumerable sequences and relays their items one after the other sequence.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source async sequence.</typeparam>
        /// <typeparam name="TResult">The element type of the inner sequences and the result items.</typeparam>
        /// <param name="source">The source async sequence to be mapped.</param>
        /// <param name="mapper">The function receiving the source item and should return an inner enumerable sequence to relay elements of.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> ConcatMap<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            return new ConcatMapEnumerable<TSource, TResult>(source, mapper);
        }

        /// <summary>
        /// Calls a handler action when the source signals an item.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="handler">The action called when an source item becomes available.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> DoOnNext<T>(this IAsyncEnumerable<T> source, Action<T> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));
            return new DoOnNext<T>(source, handler);
        }

        /// <summary>
        /// Calls an asynchronous handler function when the source
        /// signals an item and waits for this function to complete before moving on.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="handler">The function called when an item becomes available and should return a task that when completed, resumes the async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> DoOnNext<T>(this IAsyncEnumerable<T> source, Func<T, Task> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));
            return new DoOnNextAsync<T>(source, handler);
        }

        /// <summary>
        /// Converts the async sequence into a blocking enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <returns>The new IEnumerable instance.</returns>
        public static IEnumerable<T> ToEnumerable<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new ToEnumerable<T>(source);
        }

        /// <summary>
        /// Converts the async sequence into a list.
        /// </summary>
        /// <param name="source">The source async sequence.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>The task returning the new List instance.</returns>
        public static ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return ToCollection.ToList(source);
        }

        /// <summary>
        /// Converts the async sequence into an array.
        /// </summary>
        /// <param name="source">The source async sequence.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>The task returning the new Array instance.</returns>
        public static ValueTask<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return ToCollection.ToArray(source);
        }

        /// <summary>
        /// Calls a handler if the source async sequence fails to
        /// produce a fallback async sequence to resume with.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The source async sequence that can fail.</param>
        /// <param name="handler">The function taking the failure Exception and should return a fallback async
        /// sequence to resume with.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> OnErrorResumeNext<T>(this IAsyncEnumerable<T> source, Func<Exception, IAsyncEnumerable<T>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));
            return new OnErrorResumeNext<T>(source, handler);
        }

        /// <summary>
        /// Relays items of each async source of the given array one after the other.
        /// </summary>
        /// <typeparam name="T">The shared element type.</typeparam>
        /// <param name="sources">The params array of async sequences to relay one after the other.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Concat<T>(params IAsyncEnumerable<T>[] sources)
        {
            RequireNonNull(sources, nameof(sources));
            return new Concat<T>(sources);
        }

        /// <summary>
        /// Relays items of each async source of the given enumerable sequence one after the other.
        /// </summary>
        /// <typeparam name="T">The shared element type.</typeparam>
        /// <param name="sources">The enumerable sequence of async sequences to relay one after the other.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Concat<T>(IEnumerable<IAsyncEnumerable<T>> sources)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            RequireNonNull(sources, nameof(sources));
            // ReSharper disable once PossibleMultipleEnumeration
            return new ConcatEnumerable<T>(sources);
        }

        /// <summary>
        /// Continue with another async sequence once the main sequence completes.
        /// </summary>
        /// <typeparam name="T">The shared element type of the async sequences.</typeparam>
        /// <param name="source">The main source async sequences.</param>
        /// <param name="other">The next async source sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> ConcatWith<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));
            return Concat(source, other);
        }

        /// <summary>
        /// Relays items of the main async sequence until the other
        /// async sequence produces an item or completes.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <typeparam name="TOther">The element type of the other source.</typeparam>
        /// <param name="source">The main source async sequence.</param>
        /// <param name="other">The other sequence that indicates how long to relay elements of the main source.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TSource> TakeUntil<TSource, TOther>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));
            return new TakeUntil<TSource, TOther>(source, other);
        }

        /// <summary>
        /// Produces an ever increasing number periodically.
        /// </summary>
        /// <param name="period">The initial and in-between time period for when to signal the next value.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<long> Interval(TimeSpan period)
        {
            return new Interval(0, long.MinValue, period, period);
        }

        /// <summary>
        /// Produces an ever increasing number after an initial delay, then periodically.
        /// </summary>
        /// <param name="initialDelay">The initial delay before the first item emitted.</param>
        /// <param name="period">The initial and in-between time period for when to signal the next value.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<long> Interval(TimeSpan initialDelay, TimeSpan period)
        {
            return new Interval(0, long.MinValue, initialDelay, period);
        }

        /// <summary>
        /// Produces a number from a range of numbers periodically.
        /// </summary>
        /// <param name="start">The initial value.</param>
        /// <param name="count">The number of values to produce.</param>
        /// <param name="period">The initial and in-between time period for when to signal the next value.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<long> Interval(long start, long count, TimeSpan period)
        {
            return new Interval(start, start + count, period, period);
        }

        /// <summary>
        /// Produces a number from a range of numbers, the first after an initial delay and the rest periodically.
        /// </summary>
        /// <param name="start">The initial value.</param>
        /// <param name="count">The number of values to produce.</param>
        /// <param name="initialDelay">The delay before the first value is emitted.</param>
        /// <param name="period">The initial and in-between time period for when to signal the next value.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<long> Interval(long start, long count, TimeSpan initialDelay, TimeSpan period)
        {
            return new Interval(start, start + count, initialDelay, period);
        }

        /// <summary>
        /// Relays the items of that async sequence which responds first with an item or termination.
        /// </summary>
        /// <typeparam name="T">The shared element type.</typeparam>
        /// <param name="sources">The params array of async sources.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Amb<T>(params IAsyncEnumerable<T>[] sources)
        {
            RequireNonNull(sources, nameof(sources));
            return new Amb<T>(sources);
        }

        /// <summary>
        /// Ignores all elements of the source async sequence and only relays the terminal signal.
        /// </summary>
        /// <typeparam name="T">The element type of the source async sequence.</typeparam>
        /// <param name="source">The source async sequence to ignore elements of.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> IgnoreElements<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new IgnoreElements<T>(source);
        }

        /// <summary>
        /// Signal a default item if the source async sequence ends without any items.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence that could be empty.</param>
        /// <param name="defaultItem">The item to signal if the source async sequence turns out to be empty.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> DefaultIfEmpty<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));
            return new DefaultIfEmpty<T>(source, defaultItem);
        }

        /// <summary>
        /// Resumes with another async sequence if the source sequence has no items.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence that could be empty.</param>
        /// <param name="other">The fallback async sequence if the source turns out to be empty.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> SwitchIfEmpty<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));
            return new SwitchIfEmpty<T>(source, other);
        }

        /// <summary>
        /// Relays items of the source async sequence while they pass the predicate, ending the sequence when the predicate returns false.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="predicate">The function called for each source item and if returns true, the item is relayed. If it returns false, the sequence ends.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> TakeWhile<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));
            return new TakeWhile<T>(source, predicate);
        }

        /// <summary>
        /// Skips items from the source async sequence as long as the predicate returns true for them, relaying the rest once the predicate returns false.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="predicate">The function receiving the source item and should return true to keep skipping items or return false to relay the current and all subsequent items.</param>
        /// <returns></returns>
        public static IAsyncEnumerable<T> SkipWhile<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));
            return new SkipWhile<T>(source, predicate);
        }

        /// <summary>
        /// Repeatedly relay the source async sequence once it completes the previous time.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to repeatedly relay items of.</param>
        /// <param name="n">The optional number of total repeats. n == 1 will relay items once, n == 2 will relay items twice, etc.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Repeat<T>(this IAsyncEnumerable<T> source, long n = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            return new Repeat<T>(source, n, v => true);
        }

        /// <summary>
        /// Repeatedly relay the source async sequence, once it completes the previous time, when the function returns true, ending the sequence otherwise.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to repeatedly relay items of.</param>
        /// <param name="condition">The function called when the current run completes with
        /// the current run index (zero-based) and should return true to repeat the sequence once more, false to end it.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Repeat<T>(this IAsyncEnumerable<T> source, Func<long, bool> condition)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(condition, nameof(condition));
            return new Repeat<T>(source, long.MaxValue, condition);
        }

        /// <summary>
        /// Repeatedly relay the source async sequence, once it completes the previous time, when the function returns true, ending the sequence otherwise.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to repeatedly relay items of.</param>
        /// <param name="condition">The function called when the current run completes with
        /// the current run index (zero-based) and should return a task with true to repeat the sequence once more, false to end it.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Repeat<T>(this IAsyncEnumerable<T> source, Func<long, Task<bool>> condition)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(condition, nameof(condition));
            return new RepeatTask<T>(source, condition);
        }

        /// <summary>
        /// Retry a possibly failing async sequence, optionally a limited number of times before giving up.
        /// </summary>
        /// <typeparam name="T">The element type of the source async sequence.</typeparam>
        /// <param name="source">The source async sequence that could fail and should be repeated.</param>
        /// <param name="n">The number of retries. n == 1 will run the sequence once, n == 2 will run a failed sequence again.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source, long n = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            return new Retry<T>(source, n, (a, b) => true);
        }

        /// <summary>
        /// Retry a possibly failing async sequence if the condition returns true for the Exception and/or retry index.
        /// </summary>
        /// <typeparam name="T">The element type of the source async sequence.</typeparam>
        /// <param name="source">The source async sequence that could fail and should be repeated.</param>
        /// <param name="condition">Called when the sequence fails with the retry index (zero-based) and last failure Exception and should return true if the sequence should be retried.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source, Func<long, Exception, bool> condition)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(condition, nameof(condition));
            return new Retry<T>(source, long.MaxValue, condition);
        }

        /// <summary>
        /// Retry a possibly failing async sequence if the condition returns true for the Exception and/or retry index.
        /// </summary>
        /// <typeparam name="T">The element type of the source async sequence.</typeparam>
        /// <param name="source">The source async sequence that could fail and should be repeated.</param>
        /// <param name="condition">Called when the sequence fails with the retry index (zero-based) and last failure Exception and should return a task with true if the sequence should be retried.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source, Func<long, Exception, Task<bool>> condition)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(condition, nameof(condition));
            return new RetryTask<T>(source, condition);
        }

        /// <summary>
        /// Skips the last given number of items from the source
        /// async sequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="n">The number of last items to skip.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> SkipLast<T>(this IAsyncEnumerable<T> source, int n)
        {
            RequireNonNull(source, nameof(source));
            if (n == 0)
            {
                return source;
            }
            return new SkipLast<T>(source, n);
        }

        /// <summary>
        /// Keep the last number of items of the source sequence
        /// and relay those to the consumer.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="n">The number of last items to keep.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> TakeLast<T>(this IAsyncEnumerable<T> source, int n)
        {
            RequireNonNull(source, nameof(source));
            if (n == 0)
            {
                return IgnoreElements(source);
            }
            return new TakeLast<T>(source, n);
        }

        /// <summary>
        /// Collect the specified number of source items, in a non-overlapping fashion, into Lists and emit those lists.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to batch up.</param>
        /// <param name="size">The maximum number of items per lists.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<IList<T>> Buffer<T>(this IAsyncEnumerable<T> source, int size)
        {
            return Buffer(source, size, () => new List<T>());
        }

        /// <summary>
        /// Collect the specified number of source items, in a non-overlapping fashion, into custom ICollections generated on demand and emit those lists.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <typeparam name="TCollection">The custom collection type.</typeparam>
        /// <param name="source">The source async sequence to batch up.</param>
        /// <param name="size">The maximum number of items per lists.</param>
        /// <param name="bufferSupplier">The function called to create a new collection.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TCollection> Buffer<TSource, TCollection>(this IAsyncEnumerable<TSource> source, int size, Func<TCollection> bufferSupplier) where TCollection : ICollection<TSource>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(bufferSupplier, nameof(bufferSupplier));
            RequirePositive(size, nameof(size));
            return new BufferExact<TSource, TCollection>(source, size, bufferSupplier);
        }

        /// <summary>
        /// Collect the specified number of source items, possibly in an overlapping fashion, into Lists and emit those lists.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to batch up.</param>
        /// <param name="size">The maximum number of items per lists.</param>
        /// <param name="skip">After how many items to start a new list. If smaller than size, the output will be buffers sharing items. If larger than size, some source items will not be in buffers.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<IList<T>> Buffer<T>(this IAsyncEnumerable<T> source, int size, int skip)
        {
            return Buffer(source, size, skip, () => new List<T>());
        }

        /// <summary>
        /// Collect the specified number of source items, possibly in an overlapping fashion, into custom ICollections and emit those lists.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <typeparam name="TCollection">The custom collection type.</typeparam>
        /// <param name="source">The source async sequence to batch up.</param>
        /// <param name="size">The maximum number of items per lists.</param>
        /// <param name="skip">After how many items to start a new list. If smaller than size, the output will be buffers sharing items. If larger than size, some source items will not be in buffers.</param>
        /// <param name="bufferSupplier">The function called to create a new collection.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TCollection> Buffer<TSource, TCollection>(this IAsyncEnumerable<TSource> source, int size, int skip, Func<TCollection> bufferSupplier) where TCollection : ICollection<TSource>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(bufferSupplier, nameof(bufferSupplier));
            RequirePositive(size, nameof(size));
            RequirePositive(skip, nameof(skip));
            if (size == skip)
            {
                return new BufferExact<TSource, TCollection>(source, size, bufferSupplier);
            }
            if (size < skip)
            {
                return new BufferSkip<TSource, TCollection>(source, size, skip, bufferSupplier);
            }
            return new BufferOverlap<TSource, TCollection>(source, size, skip, bufferSupplier);
        }

        /// <summary>
        /// Aggregate the source elements in a rolling fashion by emitting all intermediate accumulated value.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to aggregate in a rolling fashion.</param>
        /// <param name="scanner">The function receiving the previous (or first) accumulator value, the current source item and should return the new accumulator item to be emitted as well.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Scan<T>(this IAsyncEnumerable<T> source, Func<T, T, T> scanner)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scanner, nameof(scanner));
            return new Scan<T>(source, scanner);
        }

        /// <summary>
        /// Aggregate the source elements in a rolling fashion by emitting all intermediate accumulated value.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <typeparam name="TResult">The accumulator/result type.</typeparam>
        /// <param name="source">The source async sequence to aggregate in a rolling fashion.</param>
        /// <param name="initialSupplier">The function called to generate the initial accumulator value.</param>
        /// <param name="scanner">The function receiving the previous (or first) accumulator value, the current source item and should return the new accumulator item to be emitted as well.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TResult> Scan<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TResult> initialSupplier, Func<TResult, TSource, TResult> scanner)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(initialSupplier, nameof(initialSupplier));
            RequireNonNull(scanner, nameof(scanner));
            return new ScanSeed<TSource, TResult>(source, initialSupplier, scanner);
        }

        /// <summary>
        /// Relays the first element of the source or fails with an <see cref="IndexOutOfRangeException"/> if the source async sequence is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> First<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new First<T>(source, default, false);
        }

        /// <summary>
        /// Relays the first element of the source or the default item if the source async sequence is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="defaultItem">The item to signal if the source is empty.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> First<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));
            return new First<T>(source, defaultItem, true);
        }

        /// <summary>
        /// Relays the last element of the source or fails with an <see cref="IndexOutOfRangeException"/> if the source async sequence is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Last<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new Last<T>(source, default, false);
        }

        /// <summary>
        /// Relays the last element of the source or signals the default item if the source async sequence is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="defaultItem">The item to signal if the source is empty.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Last<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));
            return new Last<T>(source, defaultItem, true);
        }

        /// <summary>
        /// Relays the only element of the source or fails with an <see cref="IndexOutOfRangeException"/> if the source async sequence is empty or has more elements.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Single<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new Single<T>(source, default, false);
        }

        /// <summary>
        /// Relays the only element of the source, the default item if the source is empty or fails with an <see cref="IndexOutOfRangeException"/> if the source has more elements.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="defaultItem">The item to signal if the source is empty.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Single<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));
            return new Single<T>(source, defaultItem, true);
        }

        /// <summary>
        /// Skips elements from the source async sequence until the other async sequence produces an item or completes, relaying all subsequent source items then on.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <typeparam name="TOther">The element type of the other/until async sequence.</typeparam>
        /// <param name="source">The source async sequence to skip items of</param>
        /// <param name="other">The other async sequence to indicate when to stop skipping and start relaying items.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TSource> SkipUntil<TSource, TOther>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));
            return new SkipUntil<TSource, TOther>(source, other);
        }

        /// <summary>
        /// Consume the source async sequence by emitting items and terminal signals via
        /// the given <see cref="IAsyncConsumer{T}"/> consumer.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <param name="source">The source sequence to consume.</param>
        /// <param name="consumer">The push-awaitable consumer.</param>
        /// <param name="ct">The optional cancellation token to stop the consumption.</param>
        /// <returns>The task that is completed when the consumption terminates.</returns>
        public static ValueTask Consume<TSource>(this IAsyncEnumerable<TSource> source, IAsyncConsumer<TSource> consumer, CancellationToken ct = default)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(consumer, nameof(consumer));
            return impl.ForEach.Consume(source, consumer, ct);
        }

        /// <summary>
        /// Groups the source values into distinct groups of async sequences.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <typeparam name="TKey">The key type of the groups.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="keySelector">The function receiving the current source item
        /// and should return a key for the group.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        /// <remarks>
        /// Note that all the groups and the main async sequence of those groups have
        /// to be consumed, otherwise the setup live-locks.
        /// </remarks>
        public static IAsyncEnumerable<IAsyncGroupedEnumerable<TKey, TSource>> GroupBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return GroupBy(source, keySelector, v => v, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Groups the source values into distinct groups of async sequences.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <typeparam name="TKey">The key type of the groups.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="keySelector">The function receiving the current source item
        /// and should return a key for the group.</param>
        /// <param name="keyComparer">The comparer for deciding which keys are equal.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        /// <remarks>
        /// Note that all the groups and the main async sequence of those groups have
        /// to be consumed, otherwise the setup live-locks.
        /// </remarks>
        public static IAsyncEnumerable<IAsyncGroupedEnumerable<TKey, TSource>> GroupBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
        {
            return GroupBy(source, keySelector, v => v, keyComparer);
        }

        /// <summary>
        /// Groups the source values into distinct groups of async sequences.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <typeparam name="TKey">The key type of the groups.</typeparam>
        /// <typeparam name="TValue">The value type of the groups.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="keySelector">The function receiving the current source item
        /// and should return a key for the group.</param>
        /// <param name="valueSelector">The function receiving the current source item
        /// and should return the value for the group.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        /// <remarks>
        /// Note that all the groups and the main async sequence of those groups have
        /// to be consumed, otherwise the setup live-locks.
        /// </remarks>
        public static IAsyncEnumerable<IAsyncGroupedEnumerable<TKey, TValue>> GroupBy<TSource, TKey, TValue>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            return GroupBy(source, keySelector, valueSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Groups the source values into distinct groups of async sequences.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <typeparam name="TKey">The key type of the groups.</typeparam>
        /// <typeparam name="TValue">The value type of the groups.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="keySelector">The function receiving the current source item
        /// and should return a key for the group.</param>
        /// <param name="valueSelector">The function receiving the current source item
        /// and should return the value for the group.</param>
        /// <param name="keyComparer">The comparer for deciding which keys are equal.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        /// <remarks>
        /// Note that all the groups and the main async sequence of those groups have
        /// to be consumed, otherwise the setup live-locks.
        /// </remarks>
        public static IAsyncEnumerable<IAsyncGroupedEnumerable<TKey, TValue>> GroupBy<TSource, TKey, TValue>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey> keyComparer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(keySelector, nameof(keySelector));
            RequireNonNull(valueSelector, nameof(valueSelector));
            RequireNonNull(keyComparer, nameof(keyComparer));
            return new GroupBy<TSource, TKey, TValue>(source, keySelector, valueSelector, keyComparer);
        }

        /// <summary>
        /// Collects all items into a List and signals it as the single result of the async sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the source and the list.</typeparam>
        /// <param name="source">The source async sequence to collect up.</param>
        /// <param name="capacityHint">The expected number of elements to avoid frequent list resizing.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<IList<T>> ToList<T>(this IAsyncEnumerable<T> source, int capacityHint = 16)
        {
            return Collect<T, IList<T>>(source, () => new List<T>(capacityHint), (a, b) => a.Add(b));
        }

        /// <summary>
        /// Periodically take the latest item from the source async sequence and relay it.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequence.</typeparam>
        /// <param name="source">The source async sequence to sample.</param>
        /// <param name="period">The sampling period.</param>
        /// <param name="emitLast">Emit the very last item, even if the sampling period has not passed.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<T> Sample<T>(this IAsyncEnumerable<T> source, TimeSpan period, bool emitLast = false)
        {
            RequireNonNull(source, nameof(source));
            return new Sample<T>(source, period, emitLast);
        }

        /// <summary>
        /// Starts buffering up to the specified amount from the source async sequence
        /// and tries to keep this buffer filled in so with (75% limit) that a slow consumer can get
        /// the items of the faster producer much earlier.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequences.</typeparam>
        /// <param name="source">The source to prefetch items of.</param>
        /// <param name="prefetch">The number of items to prefetch at most</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<T> Prefetch<T>(this IAsyncEnumerable<T> source, int prefetch)
        {
            return Prefetch(source, prefetch, prefetch - (prefetch >> 2)); // 75%
        }

        /// <summary>
        /// Starts buffering up to the specified amount from the source async sequence
        /// and tries to keep this buffer filled in so that a slow consumer can get
        /// the items of the faster producer much earlier.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequences.</typeparam>
        /// <param name="source">The source to prefetch items of.</param>
        /// <param name="prefetch">The number of items to prefetch at most</param>
        /// <param name="limit">The number of consumed items after which more items should be
        /// pulled from the source.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<T> Prefetch<T>(this IAsyncEnumerable<T> source, int prefetch, int limit)
        {
            RequireNonNull(source, nameof(source));
            RequirePositive(prefetch, nameof(prefetch));
            RequirePositive(limit, nameof(limit));
            return new Prefetch<T>(source, prefetch, limit);
        }

        /// <summary>
        /// Emit the latest item if there were no newer items from the source async
        /// sequence within the given delay period.
        /// </summary>
        /// <typeparam name="T">The element type of the source.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="delay">The time to wait after each item.</param>
        /// <param name="emitLast">If true, the very last item is emitted upon completion if
        /// the delay has not yet passed for it.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<T> Debounce<T>(this IAsyncEnumerable<T> source, TimeSpan delay, bool emitLast = false)
        {
            RequireNonNull(source, nameof(source));
            return new Debounce<T>(source, delay, emitLast);
        }

        /// <summary>
        /// Runs the source async sequence as fast as it can and samples the items
        /// as fast as the consumer can.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source async sequence to sample via "backpressure".</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));
            return new Latest<T>(source);
        }

        /// <summary>
        /// Shares and multicasts the source async sequence for the duration
        /// of a function and relays items from the returned async sequence.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source async sequence to multicast.</param>
        /// <param name="func">The function to transform the sequence without
        /// consuming it multiple times.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TResult> Publish<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<IAsyncEnumerable<TSource>, IAsyncEnumerable<TResult>> func)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(func, nameof(func));
            return new Publish<TSource, TResult>(source, func);
        }

        /// <summary>
        /// Merges the source sequence with the other sequence by
        /// running them both at the same time and serializing the item
        /// emissions.
        /// </summary>
        /// <typeparam name="TSource">The element type of the sources and result.</typeparam>
        /// <param name="source">A source to merge.</param>
        /// <param name="other">A source to merge.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> MergeWith<TSource>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TSource> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));
            return Merge(source, other);
        }

        /// <summary>
        /// Merges all async sequences running at once into a single
        /// serialized async sequence.
        /// </summary>
        /// <typeparam name="TSource">The element type of the sources and result.</typeparam>
        /// <param name="sources">The params array of source async sequences.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> Merge<TSource>(params IAsyncEnumerable<TSource>[] sources)
        {
            RequireNonNull(sources, nameof(sources));
            switch (sources.Length)
            {
                case 0:
                    return Empty<TSource>();
                case 1:
                    return sources[0];
                default:
                    return new Merge<TSource>(sources);
            }
        }

        /// <summary>
        /// Shares and multicasts the source async sequence, caching some or
        /// all of its items, for the duration
        /// of a function and relays items from the returned async sequence.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source async sequence to multicast.</param>
        /// <param name="func">The function to transform the sequence without
        /// consuming it multiple times.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TResult> Replay<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<IAsyncEnumerable<TSource>, IAsyncEnumerable<TResult>> func)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(func, nameof(func));
            return new Replay<TSource, TResult>(source, func);
        }

        /// <summary>
        /// Switches to a newer source async sequence, disposing the old one,
        /// when the source async sequence produces
        /// a value and is mapped to an async sequence via a function.
        /// </summary>
        /// <typeparam name="TSource">The source value type.</typeparam>
        /// <typeparam name="TResult">The result value type.</typeparam>
        /// <param name="source">The source to map into async sequences and switch between them.</param>
        /// <param name="mapper">The function that receives the source item and should
        /// return an async sequence to be run.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TResult> SwitchMap<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            return new SwitchMap<TSource, TResult>(source, mapper);
        }

        /// <summary>
        /// Switches to a newer source async sequence, disposing the old one,
        /// when the source async sequence produces a new inner async sequence.
        /// </summary>
        /// <typeparam name="TSource">The source value type.</typeparam>
        /// <param name="sources">The async sequence of async sequences to switch between.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> Switch<TSource>(this IAsyncEnumerable<IAsyncEnumerable<TSource>> sources)
        {
            RequireNonNull(sources, nameof(sources));
            return sources.SwitchMap(v => v);
        }

        /// <summary>
        /// Runs the inner async sequences, produced by an outer async sequence,
        /// one after the other and relays their items.
        /// </summary>
        /// <typeparam name="TSource">The value type.</typeparam>
        /// <param name="sources">The async sequence of async sequences to concatenate.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> Concat<TSource>(this IAsyncEnumerable<IAsyncEnumerable<TSource>> sources)
        {
            RequireNonNull(sources, nameof(sources));
            return sources.ConcatMap(v => v);
        }

        /// <summary>
        /// Runs the inner async sequences, produced by an outer async sequence,
        /// some or all concurrently and relays their items single a serialized async sequence.
        /// </summary>
        /// <typeparam name="TSource">The value type.</typeparam>
        /// <param name="sources">The async sequence of async sequences to merge at once.</param>
        /// <param name="maxConcurrency">The maximum number of inner sequences to run at once.</param>
        /// <param name="prefetch">The number of items to prefetch from each inner async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<TSource> Merge<TSource>(this IAsyncEnumerable<IAsyncEnumerable<TSource>> sources, int maxConcurrency = int.MaxValue, int prefetch = 32)
        {
            RequireNonNull(sources, nameof(sources));
            return sources.FlatMap(v => v, maxConcurrency, prefetch);
        }

        /// <summary>
        /// Combines the latest value of the other async sequence (if any) with
        /// the main source item via a function and emits the function's returned value.
        /// </summary>
        /// <typeparam name="TSource">The source value type.</typeparam>
        /// <typeparam name="TOther">The other value type.</typeparam>
        /// <typeparam name="TResult">The result value type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="other">The other async sequence.</param>
        /// <param name="func">The function that receives the main value and
        /// the other value whenever the main source has a new item and should
        /// return the result to be emitted to the consumer.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TResult> WithLatestFrom<TSource, TOther, TResult>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> other, Func<TSource, TOther, TResult> func)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));
            RequireNonNull(func, nameof(func));
            return new WithLatestFrom<TSource, TOther, TResult>(source, other, func);
        }

        /// <summary>
        /// Combines the latest items from each async source into a single
        /// sequence of results via a combiner function.
        /// </summary>
        /// <typeparam name="TSource">The element type of the sources.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="combiner">The function that receives the latest elements
        /// of all sources (if they all produced an item at least) and should
        /// a value to be emitted to the consumer.</param>
        /// <param name="sources">The params array of the async sequences to combine.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TResult> CombineLatest<TSource, TResult>(Func<TSource[], TResult> combiner, params IAsyncEnumerable<TSource>[] sources)
        {
            RequireNonNull(sources, nameof(sources));
            RequireNonNull(combiner, nameof(combiner));
            switch (sources.Length)
            {
                case 0:
                    return Empty<TResult>();
                case 1:
                    return sources[0].Map(v => combiner(new[] { v }));
                default:
                    return new CombineLatest<TSource, TResult>(sources, combiner);
            }
        }

        /// <summary>
        /// Buffers items from the source into Lists until the boundary
        /// sequence signals an item (or the maxSize is reached) upon which
        /// the current buffer is emitted and a new buffer is started.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <typeparam name="TOther">The element type of the boundary sequence.</typeparam>
        /// <param name="source">The source to buffer.</param>
        /// <param name="boundary">The sequence indicating the boundaries of the buffers.</param>
        /// <param name="maxSize">The maximum number of items to buffer.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<IList<TSource>> Buffer<TSource, TOther>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> boundary, int maxSize = int.MaxValue)
        {
            return Buffer<TSource, TOther, IList<TSource>>(source, boundary, () => new List<TSource>(), maxSize);
        }

        /// <summary>
        /// Buffers items from the source into custom collections until the boundary
        /// sequence signals an item (or the maxSize is reached) upon which
        /// the current collections is emitted and a new collection is started.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <typeparam name="TOther">The element type of the boundary sequence.</typeparam>
        /// <typeparam name="TCollection">The resulting collection type.</typeparam>
        /// <param name="source">The source to buffer.</param>
        /// <param name="boundary">The sequence indicating the boundaries of the buffers.</param>
        /// <param name="collectionSupplier">The custom collection supplier.</param>
        /// <param name="maxSize">The maximum number of items to buffer.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TCollection> Buffer<TSource, TOther, TCollection>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> boundary, Func<TCollection> collectionSupplier, int maxSize = int.MaxValue) where TCollection : ICollection<TSource>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(boundary, nameof(boundary));
            RequireNonNull(collectionSupplier, nameof(collectionSupplier));
            RequirePositive(maxSize, nameof(maxSize));
            return new BufferBoundaryExact<TSource, TOther, TCollection>(source, boundary, collectionSupplier, maxSize);
        }

        /// <summary>
        /// Checks if the source items have been seen before by checking if
        /// they are already successfully added to a HashSet or not, ensuring
        /// only distinct source items pass through.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result.</typeparam>
        /// <param name="source">The source to have distinct items only of.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> Distinct<TSource>(this IAsyncEnumerable<TSource> source)
        {
            return Distinct(source, v => v, () => new HashSet<TSource>());
        }

        /// <summary>
        /// Checks if the source items have been seen before by checking if
        /// they are already successfully added to a HashSet or not, ensuring
        /// only distinct source items pass through.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result.</typeparam>
        /// <param name="source">The source to have distinct items only of.</param>
        /// <param name="comparer">The comparer for comparing items in the HashSet</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> Distinct<TSource>(this IAsyncEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            RequireNonNull(comparer, nameof(comparer));
            return Distinct(source, v => v, () => new HashSet<TSource>(comparer));
        }

        /// <summary>
        /// Checks if the source items have been seen before by checking if
        /// a key extracted from such items is in a HashSet or not, ensuring
        /// only distinct source items pass through.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result.</typeparam>
        /// <typeparam name="TKey">The key type used for comparing items.</typeparam>
        /// <param name="source">The source to have distinct items only of.</param>
        /// <param name="keySelector">The function that receives the source item and should return a key value for distinct comparison.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> Distinct<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return Distinct(source, keySelector, () => new HashSet<TKey>());
        }

        /// <summary>
        /// Checks if the source items have been seen before by checking if
        /// a key extracted from such items is in a HashSet or not, ensuring
        /// only distinct source items pass through.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result.</typeparam>
        /// <typeparam name="TKey">The key type used for comparing items.</typeparam>
        /// <param name="source">The source to have distinct items only of.</param>
        /// <param name="keySelector">The function that receives the source item and should return a key value for distinct comparison.</param>
        /// <param name="keyComparer">The comparer for comparing keys in the HashSet</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> Distinct<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
        {
            RequireNonNull(keyComparer, nameof(keyComparer));
            return Distinct(source, keySelector, () => new HashSet<TKey>(keyComparer));
        }

        /// <summary>
        /// Checks if the source items have been seen before by checking
        /// a key extracted from such items against a set of keys and
        /// drops such items, ensuring that only distinct source items pass
        /// through.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result.</typeparam>
        /// <typeparam name="TKey">The key type used for comparing items.</typeparam>
        /// <param name="source">The source to have distinct items only of.</param>
        /// <param name="keySelector">The function that receives the source item and should return a key value for distinct comparison.</param>
        /// <param name="setSupplier">The function that should provide a set implementation whose <see cref="ISet{T}.Add(T)"/> method's return
        /// value decided is the source item should pass or not.</param>
        /// <returns>The new IAsyncEnumerable sequence</returns>
        public static IAsyncEnumerable<TSource> Distinct<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<ISet<TKey>> setSupplier)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(keySelector, nameof(keySelector));
            RequireNonNull(setSupplier, nameof(setSupplier));
            return new Distinct<TSource, TKey>(source, keySelector, setSupplier);
        }

        /// <summary>
        /// Checks if the current item is distinct from the previous item,
        /// and if so it is no it is relayed.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result sequences.</typeparam>
        /// <param name="source">The source sequence to filter for distinct subsequent items.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> DistinctUntilChanged<TSource>(this IAsyncEnumerable<TSource> source)
        {
            return DistinctUntilChanged(source, v => v, EqualityComparer<TSource>.Default);
        }

        /// <summary>
        /// Checks if the current item is distinct from the previous item,
        /// and if so it is no it is relayed,
        /// based on comparing the items via a custom equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result sequences.</typeparam>
        /// <param name="source">The source sequence to filter for distinct subsequent items.</param>
        /// <param name="comparer">The comparer for comparing the source items.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> DistinctUntilChanged<TSource>(this IAsyncEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            return DistinctUntilChanged(source, v => v, comparer);
        }

        /// <summary>
        /// Checks if the current item is distinct from the previous item,
        /// and if so it is no it is relayed,
        /// based on comparing keys extracted via a function.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result sequences.</typeparam>
        /// <typeparam name="TKey">The type of the keys extracted for comparison</typeparam>
        /// <param name="source">The source sequence to filter for distinct subsequent items.</param>
        /// <param name="keySelector">The function receiving the current source item and should return a key value for comparison.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> DistinctUntilChanged<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return DistinctUntilChanged(source, keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Checks if the current item is distinct from the previous item,
        /// and if so it is no it is relayed,
        /// based on comparing keys extracted via a function and using
        /// a custom equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source and result sequences.</typeparam>
        /// <typeparam name="TKey">The type of the keys extracted for comparison</typeparam>
        /// <param name="source">The source sequence to filter for distinct subsequent items.</param>
        /// <param name="keySelector">The function receiving the current source item and should return a key value for comparison.</param>
        /// <param name="keyComparer">The comparer for comparing the key values extracted via <paramref name="keySelector"/>.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> DistinctUntilChanged<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(keySelector, nameof(keySelector));
            RequireNonNull(keyComparer, nameof(keyComparer));
            return new DistinctUntilChanged<TSource, TKey>(source, keySelector, keyComparer);
        }

        /// <summary>
        /// Runs multiple async sequences mapped from the upstream source into 
        /// inner async sequences via a function at once and relays items from each, 
        /// one after the previous source terminated.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source async sequence to map and concatenate eagerly.</param>
        /// <param name="mapper">The function that receives an upstream item and should
        /// return an async sequence to be consumed once the previous async sequence terminated.</param>
        /// <param name="maxConcurrency">The maximum number of inner async sources to run at once.</param>
        /// <param name="prefetch">The number of items to prefetch from each inner source.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TResult> ConcatMapEager<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper, int maxConcurrency = 32, int prefetch = 32)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequirePositive(maxConcurrency, nameof(maxConcurrency));
            RequirePositive(prefetch, nameof(prefetch));
            return new ConcatMapEager<TSource, TResult>(source, mapper, maxConcurrency, prefetch);
        }

        /// <summary>
        /// Runs multiple async sources at once and relays items, in order, from each 
        /// one after the previous async source terminates.
        /// </summary>
        /// <typeparam name="TSource">The element type of the sources and result.</typeparam>
        /// <param name="sources">The params array of sources to run at once but concatenate in order.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> ConcatEager<TSource>(params IAsyncEnumerable<TSource>[] sources)
        {
            return FromArray(sources).ConcatMapEager(v => v);
        }

        /// <summary>
        /// Runs multiple async sources at once and relays items, in order, from each 
        /// one after the previous async source terminates.
        /// </summary>
        /// <typeparam name="TSource">The element type of the sources and result.</typeparam>
        /// <param name="maxConcurrency">The maximum number of inner async sources to run at once.</param>
        /// <param name="sources">The params array of sources to run at once but concatenate in order.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> ConcatEager<TSource>(int maxConcurrency, params IAsyncEnumerable<TSource>[] sources)
        {
            return FromArray(sources).ConcatMapEager(v => v, maxConcurrency);
        }

        /// <summary>
        /// Runs multiple async sources at once and relays items, in order, from each 
        /// one after the previous async source terminates.
        /// </summary>
        /// <typeparam name="TSource">The element type of the sources and result.</typeparam>
        /// <param name="maxConcurrency">The maximum number of inner async sources to run at once.</param>
        /// <param name="prefetch">The number of items to prefetch from each inner source.</param>
        /// <param name="sources">The params array of sources to run at once but concatenate in order.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> ConcatEager<TSource>(int maxConcurrency, int prefetch, params IAsyncEnumerable<TSource>[] sources)
        {
            return FromArray(sources).ConcatMapEager(v => v, maxConcurrency, prefetch);
        }

        /// <summary>
        /// Runs multiple async sources at once and relays items, in order, from each 
        /// one after the previous async source terminates.
        /// </summary>
        /// <typeparam name="TSource">The element type of the sources and result.</typeparam>
        /// <param name="maxConcurrency">The maximum number of inner async sources to run at once.</param>
        /// <param name="prefetch">The number of items to prefetch from each inner source.</param>
        /// <param name="sources">The params array of sources to run at once but concatenate in order.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> ConcatEager<TSource>(this IAsyncEnumerable<IAsyncEnumerable<TSource>> sources, int maxConcurrency = 32, int prefetch = 32)
        {
            return sources.ConcatMapEager(v => v, maxConcurrency, prefetch);
        }

        /// <summary>
        /// Returns the element from the async sequence with the given index (zero-based) or
        /// signals an IndexOutOfBoundsException if the sequence is shorter.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <param name="source">The source sequence to get an element of.</param>
        /// <param name="index">The index of the element, zero-based.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> ElementAt<TSource>(this IAsyncEnumerable<TSource> source, long index)
        {
            RequireNonNull(source, nameof(source));
            return new ElementAt<TSource>(source, index, default, false);
        }

        /// <summary>
        /// Returns the element from the async sequence with the given index (zero-based) or
        /// the specified default item if the sequence is shorter.
        /// </summary>
        /// <typeparam name="TSource">The element type.</typeparam>
        /// <param name="source">The source sequence to get an element of.</param>
        /// <param name="index">The index of the element, zero-based.</param>
        /// <param name="defaultItem">The default item to return if the sequence is shorter.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<TSource> ElementAt<TSource>(this IAsyncEnumerable<TSource> source, long index, TSource defaultItem)
        {
            RequireNonNull(source, nameof(source));
            return new ElementAt<TSource>(source, index, defaultItem, true);
        }

        /// <summary>
        /// Counts the number of elements in the source async sequence.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <param name="source">The source to count.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<long> Count<TSource>(this IAsyncEnumerable<TSource> source)
        {
            RequireNonNull(source, nameof(source));
            return new Count<TSource>(source);
        }

        /// <summary>
        /// Signals true if the source async sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source.</typeparam>
        /// <param name="source">The source to check for emptiness.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<bool> IsEmpty<TSource>(this IAsyncEnumerable<TSource> source)
        {
            RequireNonNull(source, nameof(source));
            return new IsEmpty<TSource>(source);
        }

        /// <summary>
        /// Signals true if any of the source items matched the predicate.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="predicate">The predicate that receives a source item and should return true if matches.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<bool> Any<TSource>(this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));
            return new Any<TSource>(source, predicate);
        }
        
        /// <summary>
        /// Signals true if any of the source items matched the async predicate.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="predicate">The predicate that receives a source item and should return a task of true if matches.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<bool> Any<TSource>(this IAsyncEnumerable<TSource> source,
            Func<TSource, Task<bool>> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));
            return new AnyTask<TSource>(source, predicate);
        }
        
        /// <summary>
        /// Signals true if all of the source items matched the predicate.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="predicate">The predicate that receives a source item and should return true if matches.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<bool> All<TSource>(this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));
            return new All<TSource>(source, predicate);
        }
        
        /// <summary>
        /// Signals true if all of the source items matched the async predicate.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="predicate">The predicate that receives a source item and should return a task of true if matches.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        public static IAsyncEnumerable<bool> All<TSource>(this IAsyncEnumerable<TSource> source,
            Func<TSource, Task<bool>> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));
            return new AllTask<TSource>(source, predicate);
        }

    }
}
