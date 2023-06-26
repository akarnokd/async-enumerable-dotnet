// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class ScanTest
    {
        [Fact]
        public async Task NoSeed_Some()
        {
            await AsyncEnumerable.Range(1, 5)
                .Scan((a, b) => a + b)
                .AssertResult(1, 3, 6, 10, 15);
        }

        [Fact]
        public async Task NoSeed_One()
        {
            await AsyncEnumerable.Just(1)
                .Scan((a, b) => a + b)
                .AssertResult(1);
        }

        [Fact]
        public async Task NoSeed_None()
        {
            await AsyncEnumerable.Empty<int>()
                .Scan((a, b) => a + b)
                .AssertResult();
        }

        [Fact]
        public async Task Seed_Some()
        {
            await AsyncEnumerable.Range(1, 5)
                .Scan(() => 100, (a, b) => a + b)
                .AssertResult(100, 101, 103, 106, 110, 115);
        }

        [Fact]
        public async Task Seed_One()
        {
            await AsyncEnumerable.Just(1)
                .Scan(() => 100, (a, b) => a + b)
                .AssertResult(100, 101);
        }

        [Fact]
        public async Task Seed_None()
        {
            await AsyncEnumerable.Empty<int>()
                .Scan(() => 100, (a, b) => a + b)
                .AssertResult(100);
        }
        
        [Fact]
        public async Task Seed_Crash()
        {
            await AsyncEnumerable.Empty<int>()
                .Scan<int, int>(() => throw new InvalidOperationException(), (a, b) => a + b)
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
