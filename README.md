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
- `Defer` - defer the creation of the actual `IAsyncEnumerable`
- `Error` - signal an error
- `FromArray` - emit the items of an array
- `FromEnumerable` - emit the items of an `IEnumerable`
- `FromTask` - emit the value returned by an async task
- `FromObservable` - convert an `IObservable` into an `IAsyncEnumerable`
- `Just` - emit a single constant value
- `Range` - emit a range of numbers
- `Timer` - emit zero after some time delay
- `Using` - use a resource for the duration of a generated actual `IAsyncEnumerable`
- `Zip` - combine the next values of multiple sources via a function and emit its results

## Intermediate operators

- `Filter` - prevent items from passing through which don't pass a predicate
- `FlatMap` - map the source items into `IAsyncEnumerable`s and merge their values into a single async sequence
- `Map` - transform one source value into some other value
- `Skip` - skip the first specified number of items of the source async sequence
- `Take` - take at most a given number of items and stop the async sequence after that
- `Timeout` - signal an error if the next item doesn't arrive within the specified time
- `ToObservable` - convert the `IAsyncEnumerable` into an `IObservable`
