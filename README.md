# async-enumerable-dotnet

Experimental operators for C# 8 [`IAsyncEnumerable`s](https://github.com/dotnet/corefx/issues/32640).

# Getting started

Namespace: `async_enumerable_dotnet`

Factory/Extension methods: `AsyncEnumerable`

```cs
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

- `Create` - generate values via async push
- `Concat` - concatenate multiple async sequences
- `Defer` - defer the creation of the actual `IAsyncEnumerable`
- `Error` - signal an error
- `Empty` - the async sequence ends without any values
- `FromArray` - emit the items of an array
- `FromEnumerable` - emit the items of an `IEnumerable`
- `FromTask` - emit the value returned by an async task
- `FromObservable` - convert an `IObservable` into an `IAsyncEnumerable`
- `Interval` - periodically signal an ever increasing number
- `Just` - emit a single constant value
- `Never` - the async sequence never produces any items and never terminates
- `Range` - emit a range of numbers
- `Timer` - emit zero after some time delay
- `Using` - use a resource for the duration of a generated actual `IAsyncEnumerable`
- `Zip` - combine the next values of multiple sources via a function and emit its results

## Intermediate operators

- `Collect` - collect items into a custom collection and emit the collection at the end
- `ConcatMap` - concatenate in order the inner async sequences mapped from the main sequence
- `ConcatWith` - concatenate in order with another async sequence
- `DoOnNext` - execute an action when an item becomes available
- `DoOnDispose` - execute an action when the async sequence gets disposed.
- `Filter` - prevent items from passing through which don't pass a predicate
- `FlatMap` - map the source items into `IAsyncEnumerable`s and merge their values into a single async sequence
- `Map` - transform one source value into some other value
- `OnErrorResumeNext` - if the main source fails, switch to an alternative source
- `Reduce` - combine elements with an accumulator and emit the last result
- `Skip` - skip the first specified number of items of the source async sequence
- `Take` - take at most a given number of items and stop the async sequence after that
- `TakeUntil` - take items from the main source until a secondary async sequence signals an item or completes
- `Timeout` - signal an error if the next item doesn't arrive within the specified time

## End-consumers

- `FirstTask` - get the very first value of the async sequence
- `ForEach` - invoke callbacks for each item and for the terminal signals
- `LastTask` - get the very last value of the sequence
- `SingleTask` - get the only value of the sequence or signal error
- `ToEnumerable` - convert the `IAsyncEnumerable` into a blocking `IEnumerable`
- `ToObservable` - convert the `IAsyncEnumerable` into an `IObservable`
