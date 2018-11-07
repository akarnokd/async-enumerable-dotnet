// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System;

namespace async_enumerable_dotnet_test
{
    public class SwitchMapTest
    {
        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .SwitchMap(v => AsyncEnumerable.Range(1, 5))
                .AssertResult();
        }

        [Fact]
        public async void Single()
        {
            await AsyncEnumerable.Just(2)
                .SwitchMap(v => AsyncEnumerable.Range(v, 5))
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Fact]
        public async void Many_Switched()
        {
            await AsyncEnumerable.Range(1, 5)
                .SwitchMap(v => AsyncEnumerable.Range(v * 10, 5))
                .Last()
                .AssertResult(54);
        }


        [Fact]
        public async void Many_Switched_Lots()
        {
            await AsyncEnumerable.Range(1, 1000)
                .SwitchMap(v => AsyncEnumerable.Range(v * 1000, 100))
                .Last()
                .AssertResult(1_000_099);
        }

        [Fact]
        public async void Many_Switched_Lots_2()
        {
            await AsyncEnumerable.Range(1, 100_000)
                .SwitchMap(v => AsyncEnumerable.Range(v, 2))
                .Last()
                .AssertResult(100_001);
        }

        [Fact]
        public async void Error_Outer()
        {
            await AsyncEnumerable.Just(2).WithError(new InvalidOperationException())
                .SwitchMap(v => AsyncEnumerable.Range(v, 5))
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Fact]
        public async void Error_Inner()
        {
            await AsyncEnumerable.Just(2)
                .SwitchMap(v => AsyncEnumerable.Range(v, 5)
                        .WithError(new InvalidOperationException())
                )
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Fact]
        public async void Mapper_Crash()
        {
            await AsyncEnumerable.Just(2)
                .SwitchMap<int, int>(v => throw new InvalidOperationException())
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Nested()
        {
            await AsyncEnumerable.Just(AsyncEnumerable.Range(2, 5))
                .Switch()
                .AssertResult(2, 3, 4, 5, 6);
        }
    }
}
