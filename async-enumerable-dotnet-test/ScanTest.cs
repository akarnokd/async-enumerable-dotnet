// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class ScanTest
    {
        [Fact]
        public async void NoSeed_Some()
        {
            await AsyncEnumerable.Range(1, 5)
                .Scan((a, b) => a + b)
                .AssertResult(1, 3, 6, 10, 15);
        }

        [Fact]
        public async void NoSeed_One()
        {
            await AsyncEnumerable.Just(1)
                .Scan((a, b) => a + b)
                .AssertResult(1);
        }

        [Fact]
        public async void NoSeed_None()
        {
            await AsyncEnumerable.Empty<int>()
                .Scan((a, b) => a + b)
                .AssertResult();
        }

        [Fact]
        public async void Seed_Some()
        {
            await AsyncEnumerable.Range(1, 5)
                .Scan(() => 100, (a, b) => a + b)
                .AssertResult(100, 101, 103, 106, 110, 115);
        }

        [Fact]
        public async void Seed_One()
        {
            await AsyncEnumerable.Just(1)
                .Scan(() => 100, (a, b) => a + b)
                .AssertResult(100, 101);
        }

        [Fact]
        public async void Seed_None()
        {
            await AsyncEnumerable.Empty<int>()
                .Scan(() => 100, (a, b) => a + b)
                .AssertResult(100);
        }
        
        [Fact]
        public async void Seed_Crash()
        {
            await AsyncEnumerable.Empty<int>()
                .Scan<int, int>(() => throw new InvalidOperationException(), (a, b) => a + b)
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
