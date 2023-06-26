// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class DistinctUntilChangedTest
    {
        [Fact]
        public async Task Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .DistinctUntilChanged()
                .AssertResult();
        }

        [Fact]
        public async Task Just()
        {
            await AsyncEnumerable.Just(1)
                .DistinctUntilChanged()
                .AssertResult(1);
        }

        [Fact]
        public async Task Range()
        {
            await AsyncEnumerable.Range(1, 5)
                .DistinctUntilChanged()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task Redundant()
        {
            await AsyncEnumerable.FromArray(1, 2, 3, 3, 2, 1, 1, 4, 5, 4, 4)
                .DistinctUntilChanged()
                .AssertResult(1, 2, 3, 2, 1, 4, 5, 4);
        }

        [Fact]
        public async Task Redundant_Comparer()
        {
            await AsyncEnumerable.FromArray(1, 2, 3, 3, 2, 1, 1, 4, 5, 4, 4)
                .DistinctUntilChanged(EqualityComparer<int>.Default)
                .AssertResult(1, 2, 3, 2, 1, 4, 5, 4);
        }

        [Fact]
        public async Task KeySelector()
        {
            await AsyncEnumerable.Range(1, 10)
                .DistinctUntilChanged(k => k / 3)
                .AssertResult(1, 3, 6, 9);
        }

        [Fact]
        public async Task KeySelector_KeyComparer()
        {
            await AsyncEnumerable.Range(1, 10)
                .DistinctUntilChanged(k => k / 3, EqualityComparer<long>.Default)
                .AssertResult(1, 3, 6, 9);
        }
    }
}
