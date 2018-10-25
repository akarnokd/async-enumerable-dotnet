using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Combine a row of the next elements from each async sequence via a zipper function.
        /// </summary>
        /// <typeparam name="T">The common element type of the sources.</typeparam>
        /// <typeparam name="R">The resulting element type.</typeparam>
        /// <param name="zipper">The function that receives the next items from the sources in
        /// an array and should return the value to be passed forward.</param>
        /// <param name="sources">The params array of async sequences.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<R> Zip<T, R>(Func<T[], R> zipper, params IAsyncEnumerable<T>[] sources)
        {
            return new ZipArray<T, R>(sources, zipper);
        }

        /// <summary>
        /// Wraps an array of items into an async sequence that emits its elements.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="values">The params array of values to emit.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> FromArray<T>(params T[] values)
        {
            return new FromArray<T>(values);
        }

        /// <summary>
        /// Signals a <see cref="TimeoutException"/> when more than the specified amount of
        /// time passes between items.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source sequence to relay items of.</param>
        /// <param name="timeout">The timeout between items.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Timeout<T>(this IAsyncEnumerable<T> source, TimeSpan timeout)
        {
            return new Timeout<T>(source, timeout);
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
        /// such as <see cref="Defer"/>.
        /// </remarks>
        public static IAsyncEnumerable<long> Timer(TimeSpan delay, CancellationToken token)
        {
            return new TimerCancellable(delay, token);
        }

        /// <summary>
        /// Calls the specified synchronous handler when the async sequence is disposed via
        /// <see cref="IAsyncDisposable.DisposeAsync"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source sequence to relay items of.</param>
        /// <param name="handler">The handler called when the sequence is disposed.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> DoOnDispose<T>(this IAsyncEnumerable<T> source, Action handler)
        {
            return new DoOnDispose<T>(source, handler);
        }

        /// <summary>
        /// Calls the specified asynchronous handler when the async sequence is disposed via
        /// <see cref="IAsyncDisposable.DisposeAsync"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source sequence to relay items of.</param>
        /// <param name="handler">The async handler called when the sequence is disposed.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> DoOnDisposeAsync<T>(this IAsyncEnumerable<T> source, Func<ValueTask> handler)
        {
            return new DoOnDisposeAsync<T>(source, handler);
        }

        /// <summary>
        /// Signals the given Exception immediately.
        /// </summary>
        /// <typeparam name="T">The intended element type of the sequence.</typeparam>
        /// <param name="ex">The exception to signal immediately.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Error<T>(Exception ex)
        {
            return new Error<T>(ex);
        }

        /// <summary>
        /// Creates a resource and an actual async sequence with the help of this resource
        /// to be relayed and cleaned up afterwards.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequence.</typeparam>
        /// <typeparam name="R">The resource type.</typeparam>
        /// <param name="resourceProvider">The function that returns the resource to be used.</param>
        /// <param name="sourceProvider">The function that produces the actual async sequence for
        /// the generated resource.</param>
        /// <param name="resourceCleanup">The action to cleanup the resource after the generated
        /// async sequence terminated.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Using<T, R>(Func<R> resourceProvider, Func<R, IAsyncEnumerable<T>> sourceProvider, Action<R> resourceCleanup)
        {
            return new Using<T, R>(resourceProvider, sourceProvider, resourceCleanup);
        }

        /// <summary>
        /// Create a task and emit its result/error as an async sequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="func">The function that returns a task that will create an item.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> FromTask<T>(Func<Task<T>> func)
        {
            return new FromTaskFunc<T>(func);
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
            return new Range(start, start + count);
        }

        /// <summary>
        /// Maps the items of the source async sequence into async sequences and then
        /// merges the elements of those inner sequences into a one sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the source async sequence.</typeparam>
        /// <typeparam name="R">The element type of the inner async sequences.</typeparam>
        /// <param name="source">The source that emits items to be mapped.</param>
        /// <param name="mapper">The function that takes an source item and should return
        /// an async sequence to be merged.</param>
        /// <param name="maxConcurrency">The maximum number of inner sequences to run at once.</param>
        /// <param name="prefetch">The number of items to prefetch from each inner async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<R> FlatMap<T, R>(this IAsyncEnumerable<T> source, Func<T, IAsyncEnumerable<R>> mapper, int maxConcurrency = int.MaxValue, int prefetch = 32)
        {
            return new FlatMap<T, R>(source, mapper, maxConcurrency, prefetch);
        }

        /// <summary>
        /// Transforms each souce item into another item via a function.
        /// </summary>
        /// <typeparam name="T">The element type of the source.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="source">The source async sequence to transform.</param>
        /// <param name="mapper">The function that takes a source item and should return the result item
        /// to be emitted.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<R> Map<T, R>(this IAsyncEnumerable<T> source, Func<T, R> mapper)
        {
            return new Map<T, R>(source, mapper);
        }

        /// <summary>
        /// Transforms each souce item into another item via an asynchronous function.
        /// </summary>
        /// <typeparam name="T">The element type of the source.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="source">The source async sequence to transform.</param>
        /// <param name="mapper">The function that takes a source item and should return a task that produces
        /// the result item.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<R> Map<T, R>(this IAsyncEnumerable<T> source, Func<T, Task<R>> mapper)
        {
            return new MapAsync<T, R>(source, mapper);
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
            return new FromEnumerable<T>(source);
        }

        /// <summary>
        /// Creates an async sequence that emits the given pre-existing value.
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
        /// <param name="source">The source asnyc sequence.</param>
        /// <param name="n">The number of items to skip.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> source, long n)
        {
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
        /// <returns>The shared non-signalling IAsyncEnumerable instance.</returns>
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
            return impl.ForEach.ForEachAction<T>(source, onNext, onError, onComplete);
        }

        /// <summary>
        /// Returns a task that produces the first item of the source async sequence
        /// or an <see cref="IndexOutOfRangeException"/> if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the first item of.</param>
        /// <returns>The task that completes with the first item of the sequence or fails.</returns>
        public static ValueTask<T> FirstTask<T>(this IAsyncEnumerable<T> source)
        {
            return impl.FirstLastSingleTask.First(source, default, false);
        }

        /// <summary>
        /// Returns a task that produces the first item of the source async sequence
        /// or the given default item if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the first item of.</param>
        /// <param name="defaultItem">The item to return if the source is empty.</param>
        /// <returns>The task that completes with the first/default item of the sequence or fails.</returns>
        public static ValueTask<T> FirstTask<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            return impl.FirstLastSingleTask.First(source, defaultItem, true);
        }

        /// <summary>
        /// Returns a task that produces the last item of the source async sequence
        /// or an <see cref="IndexOutOfRangeException"/> if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the last item of.</param>
        /// <returns>The task that completes with the last item of the sequence or fails.</returns>
        public static ValueTask<T> LastTask<T>(this IAsyncEnumerable<T> source)
        {
            return impl.FirstLastSingleTask.Last(source, default, false);
        }

        /// <summary>
        /// Returns a task that produces the last item of the source async sequence
        /// or the given default item if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the last item of.</param>
        /// <param name="defaultItem">The item to return if the source is empty.</param>
        /// <returns>The task that completes with the last/default item of the sequence or fails.</returns>
        public static ValueTask<T> LastTask<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            return impl.FirstLastSingleTask.Last(source, defaultItem, true);
        }

        /// <summary>
        /// Returns a task that produces the only item of the source async sequence
        /// or an <see cref="IndexOutOfRangeException"/> if the source is empty or has more items.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the only item of.</param>
        /// <returns>The task that completes with the only item of the sequence or fails.</returns>
        public static ValueTask<T> SingleTask<T>(this IAsyncEnumerable<T> source)
        {
            return impl.FirstLastSingleTask.Single(source, default, false);
        }

        /// <summary>
        /// Returns a task that produces the only item of the source async sequence
        /// or the given default item if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to get the only item of.</param>
        /// <param name="defaultItem">The item to return if the source is empty.</param>
        /// <returns>The task that completes with the only/default item of the sequence or fails.</returns>
        public static ValueTask<T> SingleTask<T>(this IAsyncEnumerable<T> source, T defaultItem)
        {
            return impl.FirstLastSingleTask.Single(source, defaultItem, true);
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
            return new Reduce<T>(source, reducer);
        }

        /// <summary>
        /// Combines an accumulator and the next item through a function to produce
        /// a new accumulator of which the last accumulator value
        /// is the result item.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="R">The accumulator and result type.</typeparam>
        /// <param name="source">The source to reduce into a single value.</param>
        /// <param name="initialSupplier">The function returning the initial accumulator value.</param>
        /// <param name="reducer">The function that takes the previous accumulator value (or the first item), the current item and should produce the new accumulator value.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<R> Reduce<T, R>(this IAsyncEnumerable<T> source, Func<R> initialSupplier, Func<R, T, R> reducer)
        {
            return new ReduceSeed<T, R>(source, initialSupplier, reducer);
        }

        /// <summary>
        /// Generates a collection and calls an action with
        /// the current item to be combined into it and then
        /// the collection is emitted as the final result.
        /// </summary>
        /// <typeparam name="T">The element type of the source async sequence.</typeparam>
        /// <typeparam name="C">The collection and result type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <param name="collectionSupplier">The function that generates the collection.</param>
        /// <param name="collector">The action called with the collection and the current source item.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<C> Collect<T, C>(this IAsyncEnumerable<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            return new Collect<T, C>(source, collectionSupplier, collector);
        }

        /// <summary>
        /// Maps the source items into inner async sequences and relays their items one after the other sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the source async sequence.</typeparam>
        /// <typeparam name="R">The element type of the inner sequences and the result items.</typeparam>
        /// <param name="source">The source async sequence to be mapped.</param>
        /// <param name="mapper">The function recieving the source item and should return an inner async sequence to relay elements of.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<R> ConcatMap<T, R>(this IAsyncEnumerable<T> source, Func<T, IAsyncEnumerable<R>> mapper)
        {
            return new ConcatMap<T, R>(source, mapper);
        }

        /// <summary>
        /// Maps the source items into inner enumerable sequences and relays their items one after the other sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the source async sequence.</typeparam>
        /// <typeparam name="R">The element type of the inner sequences and the result items.</typeparam>
        /// <param name="source">The source async sequence to be mapped.</param>
        /// <param name="mapper">The function recieving the source item and should return an inner enumerable sequence to relay elements of.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<R> ConcatMap<T, R>(this IAsyncEnumerable<T> source, Func<T, IEnumerable<R>> mapper)
        {
            return new ConcatMapEnumerable<T, R>(source, mapper);
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
            return new ToEnumerable<T>(source);
        }

        /// <summary>
        /// Converts the async sequence into a list.
        /// </summary>
        /// <param name="source">The source async sequence.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>The task returning the new List instance.</returns>
        public static ValueTask<List<T>> ToList<T>(this IAsyncEnumerable<T> source)
        {
            return ToCollection.ToList(source);
        }

        /// <summary>
        /// Converts the async sequence into an array.
        /// </summary>
        /// <param name="source">The source async sequence.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>The task returning the new Array instance.</returns>
        public static ValueTask<T[]> ToArray<T>(this IAsyncEnumerable<T> source)
        {
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
            return Concat(source, other);
        }

        /// <summary>
        /// Relays items of the main async sequence until the other
        /// async sequence produces an item or completes.
        /// </summary>
        /// <typeparam name="T">The source element type.</typeparam>
        /// <typeparam name="U">The element type of the other source.</typeparam>
        /// <param name="source">The main source async sequence.</param>
        /// <param name="other">The other sequence that indicates how long to relay elements of the main source.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> TakeUntil<T, U>(this IAsyncEnumerable<T> source, IAsyncEnumerable<U> other)
        {
            return new TakeUntil<T, U>(source, other);
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
        /// Produces an ever increasing number after an itial delay, then periodically.
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
            return new Repeat<T>(source, n, v => true);
        }

        /// <summary>
        /// Repeatedly relay the source async sequence, once it completes the previous time, when the function returns true, ending the sequence otherwise.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence to repeatedly relay items of.</param>
        /// <param name="condition">The function called when the current run completes with
        /// the current run index (zero-based) and should return to repeat the sequence once more, false to end it.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> Repeat<T>(this IAsyncEnumerable<T> source, Func<long, bool> condition)
        {
            return new Repeat<T>(source, long.MaxValue, condition);
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
            return new Retry<T>(source, long.MaxValue, condition);
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
            if (n == 0)
            {
                return IgnoreElements(source);
            }
            return new TakeLast<T>(source, n);
        }

        /// <summary>
        /// Collect the specified number of source items, non-overlappingly, into Lists and emit those lists.
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
        /// Collect the specified number of source items, non-overlappingly, into custom ICollections generated on demand and emit those lists.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="C">The custom collection type.</typeparam>
        /// <param name="source">The source async sequence to batch up.</param>
        /// <param name="size">The maximum number of items per lists.</param>
        /// <param name="bufferSupplier">The function called to create a new collection.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<C> Buffer<T, C>(this IAsyncEnumerable<T> source, int size, Func<C> bufferSupplier) where C : ICollection<T>
        {
            return new BufferExact<T, C>(source, size, bufferSupplier);
        }

        /// <summary>
        /// Collect the specified number of source items, possibly overlappingly, into Lists and emit those lists.
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
        /// Collect the specified number of source items, possibly overlappingly, into custom ICollections and emit those lists.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="C">The custom collection type.</typeparam>
        /// <param name="source">The source async sequence to batch up.</param>
        /// <param name="size">The maximum number of items per lists.</param>
        /// <param name="skip">After how many items to start a new list. If smaller than size, the output will be buffers sharing items. If larger than size, some source items will not be in buffers.</param>
        /// <param name="bufferSupplier">The function called to create a new collection.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<C> Buffer<T, C>(this IAsyncEnumerable<T> source, int size, int skip, Func<C> bufferSupplier) where C : ICollection<T>
        {
            if (size == skip)
            {
                return new BufferExact<T, C>(source, size, bufferSupplier);
            }
            else if (size < skip)
            {
                return new BufferSkip<T, C>(source, size, skip, bufferSupplier);
            }
            return new BufferOverlap<T, C>(source, size, skip, bufferSupplier);
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
            return new Scan<T>(source, scanner);
        }

        /// <summary>
        /// Aggregate the source elements in a rolling fashion by emitting all intermediate accumulated value.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="R">The accumulator/result type.</typeparam>
        /// <param name="source">The source async sequence to aggregate in a rolling fashion.</param>
        /// <param name="initialSupplier">The function called to generate the initial accumulator value.</param>
        /// <param name="scanner">The function receiving the previous (or first) accumulator value, the current source item and should return the new accumulator item to be emitted as well.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<R> Scan<T, R>(this IAsyncEnumerable<T> source, Func<R> initialSupplier, Func<R, T, R> scanner)
        {
            return new ScanSeed<T, R>(source, initialSupplier, scanner);
        }

        /// <summary>
        /// Relays the first element of the source or fails with an <see cref="IndexOutOfRangeException"/> if the source async sequence is empty.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source async sequence.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> First<T>(this IAsyncEnumerable<T> source)
        {
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
            return new Single<T>(source, defaultItem, true);
        }

        /// <summary>
        /// Skips elements from the source async sequence until the other async sequence produces an item or completes, relaying all subsequent source items then on.
        /// </summary>
        /// <typeparam name="T">The element type of the source.</typeparam>
        /// <typeparam name="U">The element type of the other/until async sequence.</typeparam>
        /// <param name="source">The source async sequence to skip items of</param>
        /// <param name="other">The other async sequence to indicate when to stop skipping and start relaying items.</param>
        /// <returns>The new IAsyncEnumerable instance.</returns>
        public static IAsyncEnumerable<T> SkipUntil<T, U>(this IAsyncEnumerable<T> source, IAsyncEnumerable<U> other)
        {
            return new SkipUntil<T, U>(source, other);
        }

        /// <summary>
        /// Consume the source async sequence by emitting items and terminal signals via
        /// the given <see cref="IAsyncConsumer{T}"/> consumer.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source sequence to consume.</param>
        /// <param name="consumer">The push-awaitable consumer.</param>
        /// <param name="ct">The optional cancellation token to stop the consumption.</param>
        /// <returns>The task that is completed when the consumption terminates.</returns>
        public static ValueTask Consume<T>(this IAsyncEnumerable<T> source, IAsyncConsumer<T> consumer, CancellationToken ct = default)
        {
            return impl.ForEach.Consume(source, consumer, ct);
        }
    }
}
