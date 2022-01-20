// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Linq;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class ConcatMapTest
    {
        [Fact]
        public async Task Async_Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMap(v => AsyncEnumerable.Range(v * 10, 5))
                .AssertResult(
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async Task Async_Filter()
        {
            await AsyncEnumerable.Range(1, 10)
                .ConcatMap(v => v % 2 == 0 ? AsyncEnumerable.Just(v) : AsyncEnumerable.Empty<int>())
                .AssertResult(
                    2, 4, 6, 8, 10
                );
        }

        [Fact]
        public async Task Async_Take()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMap(v => AsyncEnumerable.Range(v * 10, 5))
                .Take(7)
                .AssertResult(
                    10, 11, 12, 13, 14,
                    20, 21
                );
        }

        [Fact]
        public async Task Enumerable_Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMap(v => Enumerable.Range(v * 10, 5))
                .AssertResult(
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async Task Enumerable_Filter()
        {
            await AsyncEnumerable.Range(1, 10)
                .ConcatMap(v => v % 2 == 0 ? new[] { v } : Enumerable.Empty<int>())
                .AssertResult(
                    2, 4, 6, 8, 10
                );
        }

        [Fact]
        public async Task Enumerable_Take()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMap(v => Enumerable.Range(v * 10, 5))
                .Take(7)
                .AssertResult(
                    10, 11, 12, 13, 14,
                    20, 21
                );
        }
    }
}
