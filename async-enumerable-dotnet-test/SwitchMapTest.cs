// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class SwitchMapTest
    {
        [Fact]
        public async Task Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .SwitchMap(v => AsyncEnumerable.Range(1, 5))
                .AssertResult();
        }

        [Fact]
        public async Task Single()
        {
            await AsyncEnumerable.Just(2)
                .SwitchMap(v => AsyncEnumerable.Range(v, 5))
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Fact]
        public async Task Many_Switched()
        {
            await AsyncEnumerable.Range(1, 5)
                .SwitchMap(v => AsyncEnumerable.Range(v * 10, 5))
                .Last()
                .AssertResult(54);
        }


        [Fact]
        public async Task Many_Switched_Lots()
        {
            await AsyncEnumerable.Range(1, 1000)
                .SwitchMap(v => AsyncEnumerable.Range(v * 1000, 100))
                .Last()
                .AssertResult(1_000_099);
        }

        [Fact]
        public async Task Many_Switched_Lots_2()
        {
            await AsyncEnumerable.Range(1, 100_000)
                .SwitchMap(v => AsyncEnumerable.Range(v, 2))
                .Last()
                .AssertResult(100_001);
        }

        [Fact]
        public async Task Error_Outer()
        {
            await AsyncEnumerable.Just(2).WithError(new InvalidOperationException())
                .SwitchMap(v => AsyncEnumerable.Range(v, 5))
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Fact]
        public async Task Error_Inner()
        {
            await AsyncEnumerable.Just(2)
                .SwitchMap(v => AsyncEnumerable.Range(v, 5)
                        .WithError(new InvalidOperationException())
                )
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Fact]
        public async Task Mapper_Crash()
        {
            await AsyncEnumerable.Just(2)
                .SwitchMap<int, int>(v => throw new InvalidOperationException())
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async Task Nested()
        {
            await AsyncEnumerable.Just(AsyncEnumerable.Range(2, 5))
                .Switch()
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Fact]
        public async Task NoCancelDelay()
        {
            var start = DateTime.Now;
            try
            {
                await foreach (var item in async_enumerable_dotnet.AsyncEnumerable.Interval(TimeSpan.Zero, TimeSpan.FromSeconds(10))
                .Map(x => async_enumerable_dotnet.AsyncEnumerable.Just(x))
                .Switch())
                {
                    Console.WriteLine(item);

                    throw new Exception("expected");
                }

                throw new Exception("unexpected");
            } catch (Exception e) when (e.Message == "expected")
            {
                // expected
            }
            var end = DateTime.Now;

            if (end - start > TimeSpan.FromSeconds(5))
            {
                Assert.True(false, "Test took too much time");
            }
        }
    }
}
