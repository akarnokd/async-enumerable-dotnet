# async-enumerable-dotnet

Experimental operators for C# 8 [`IAsyncEnumerable`s](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=dotnet-plat-ext-3.0).

Travis-CI: <a href='https://travis-ci.com/akarnokd/async-enumerable-dotnet/builds'><img src='https://travis-ci.com/akarnokd/async-enumerable-dotnet.svg?branch=master' alt="async-enumerable-dotnet"></a>
NuGet: <a href='https://www.nuget.org/packages/akarnokd.async-enumerable-dotnet'><img src='https://img.shields.io/nuget/v/akarnokd.async-enumerable-dotnet.svg' alt="async-enumerable-dotnet"/></a>

# Getting started

Namespace: `async_enumerable_dotnet`

Factory/Extension methods: `AsyncEnumerable`

Requires: .NET Core 3 or later

```cs
using async_enumerable_dotnet;
using System.Collections.Generic;

var result = AsyncEnumerable.Range(1, 10)
    .Filter(v => v % 2 == 0)
    .Map(v => v * 2)
    .Take(5)
    ;

var enumerator = result.GetAsyncEnumerator();

try
{
    while (await enumerator.MoveNextAsync()) 
    {
        Console.WriteLine(enumerator.Current);
    }
	Console.WriteLine("Done");
}
finally
{
    await enumerator.DisposeAsync();
}
```

## Available sources

- `Amb` - Relay items of the source that responds first, disposing the others
- `Create` - generate values via async push
- `CombineLatest` - combines the latest items of the source async sequences via a function into results
- `Concat` - concatenate multiple async sequences
- `ConcatEager` - run multiple async sequences at once but relay elements in order similar to `Concat`
- `Defer` - defer the creation of the actual `IAsyncEnumerable`
- `Error` - signal an error
- `Empty` - the async sequence ends without any values
- `FromArray` - emit the items of an array
- `FromEnumerable` - emit the items of an `IEnumerable`
- `FromTask` - emit the value returned by an async task
- `FromObservable` - convert an `IObservable` into an `IAsyncEnumerable`
- `Interval` - periodically signal an ever increasing number
- `Just` - emit a single constant value
- `Merge` - run multiple sources at once and merge their items into a single async sequence
- `Never` - the async sequence never produces any items and never terminates
- `Range` - emit a range of numbers
- `Switch` - switch between inner async sources produced by an outer async sequence
- `Timer` - emit zero after some time delay
- `Using` - use a resource for the duration of a generated actual `IAsyncEnumerable`
- `Zip` - combine the next values of multiple sources via a function and emit its results

## Intermediate operators

- `Any` - signals true if any of the source items matched a predicate
- `All` - signals true if all of the source items matched a predicate
- `Buffer` - collect some number of items into buffer(s) and emit those buffers
- `Collect` - collect items into a custom collection and emit the collection at the end
- `ConcatMap` - concatenate in order the inner async sequences mapped from the main sequence
- `ConcatMapEager` - run the async sources at once but relay elements in order similar to `ConcatMap`
- `ConcatWith` - concatenate in order with another async sequence
- `Count` - count the number of items in the async sequence
- `Distinct` - makes sure only distinct elements get relayed
- `DistinctUntilChanged` - relays an element only if it is distinct from the previous item
- `Debounce` - wait a bit after each item and emit them if no newer item arrived from the source
- `DefaultIfEmpty` - return a fallback value if the source async sequence turns out to be empty
- `DoOnNext` - execute an action when an item becomes available
- `DoOnDispose` - execute an action when the async sequence gets disposed.
- `ElementAt` - get the element at a specified index or an error/default value
- `Filter` - prevent items from passing through which don't pass a predicate
- `First` - signals the first item of the async sequence
- `FlatMap` - map the source items into `IAsyncEnumerable`s and merge their values into a single async sequence
- `GroupBy` - groups the source elements into distinct async groups
- `IgnoreElements` - ignores items and ends when the source async sequence ends
- `IsEmpty` - signals a single true if the source is empty
- `Last` - signals the last item of the async sequence
- `Latest` - runs the source async sequence as fast as it can and samples it with the frequency of the consumer
- `Map` - transform one source value into some other value
- `MergeWith` - run two async sources at once and merge their items into a single async sequence
- `OnErrorResumeNext` - if the main source fails, switch to an alternative source
- `Prefetch` - run the source async sequence to prefetch items for a slow consumer
- `Publish` - consume an async sequence once while multicasting its items to intermediate consumers for the duration of a function.
- `Reduce` - combine elements with an accumulator and emit the last result
- `Repeat` - repeatedly consume the entire source async sequence (up to a number of times and/or condition)
- `Replay` - consume an async sequence once, caching some or all of its items and multicasting them to intermediate consumers for the duration of a function.
- `Retry` - retry a failed async sequence (up to a number of times or based on condition)
- `Sample` - periodically take the latest item from the source sequence and emit it
- `Scan` - perform rolling aggregation by emitting intermediate results
- `Single` - signals the only item of the async sequence, fails if the sequence has more than one item
- `Skip` - skip the first specified number of items of the source async sequence
- `SkipLast` - skip the last number of elements
- `SkipUntil` - skip until another async sequence signals an item or completes
- `SkipWhile` - skip items while the predicate returns true, start emitting when it turns false
- `SwitchIfEmpty` - switch to an alternate async sequence if the main sequence turns out to be empty
- `SwitchMap` - switch to a newer mapped-in async sequence, disposing the old one, whenever the source produces an item
- `Take` - take at most a given number of items and stop the async sequence after that
- `TakeLast` - take the last given number of items of the source async sequence and emit those
- `TakeUntil` - take items from the main source until a secondary async sequence signals an item or completes
- `TakeWhile` - take items while predicate is true and stop when it turns false
- `Timeout` - signal an error if the next item doesn't arrive within the specified time
- `ToList` - collects all items into a List and signals it as the single result of the async sequence
- `WithLatestFrom` - combines the elements of the main sequence with the latest value(s) from other sequence(s)

## End-consumers

- `Consume` - consume the async sequence via a awaitable push interface of `IAsyncConsumer`
- `FirstAsync` - get the very first value of the async sequence
- `ForEach` - invoke callbacks for each item and for the terminal signals
- `LastAsync` - get the very last value of the sequence
- `SingleAsync` - get the only value of the sequence or signal error
- `ToArrayAsync` - get all items as an array
- `ToEnumerable` - convert the `IAsyncEnumerable` into a blocking `IEnumerable`
- `ToListAsync` - get all items in an IList
- `ToObservable` - convert the `IAsyncEnumerable` into an `IObservable`

## Push-pull bridges

- `MulticastAsyncEnumerable` - signals events to currently associated IAsyncEnumerator consumers (aka PublishSubject).
- `ReplayAsyncEnumerable` - replays some or all items to its IAsyncEnumerator consumers (aka ReplaySubject).
- `UnicastAsyncEnumerable` - buffers then replay items for an only consumer

## Other components

- `TestTaskRunner` - a class that creates tasks (of value, error or cancellation) that signal when a virtual time is moved forward (aka TestScheduler)

### IAsyncConsumer

Represents a push-like consumer where items, an error and/or completion can be signaled and awaited:

```cs
public interface IAsyncConsumer<in T>
{
    ValueTask Next(T item);

    ValueTask Error(Exception error);

    ValueTask Complete();
}
```

The methods must be awaited and called non-concurrently and non-overlappingly with themselves and each other:

```
Next* (Error | Complete)?
```
