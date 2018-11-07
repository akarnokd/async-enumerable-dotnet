// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class ReduceTest
    {
        [Fact]
        public async void NoSeed_Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .Reduce((a, b) => a + b)
                .AssertResult(15);
        }

        [Fact]
        public async void NoSeed_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Reduce((a, b) => a + b)
                .AssertResult();
        }

        [Fact]
        public async void WithSeed_Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .Reduce(() => 10, (a, b) => a + b)
                .AssertResult(25);
        }

        [Fact]
        public async void WithSeed_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Reduce(() => 10, (a, b) => a + b)
                .AssertResult(10);
        }
        
        [Fact]
        public async void WithSeed_Crash()
        {
            await AsyncEnumerable.Empty<int>()
                .Reduce<int, int>(() => throw new InvalidOperationException(), (a, b) => a + b)
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
