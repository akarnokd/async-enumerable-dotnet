// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Collections.Generic;

namespace async_enumerable_dotnet_test
{
    public class DistinctUntilChangedTest
    {
        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .DistinctUntilChanged()
                .AssertResult();
        }

        [Fact]
        public async void Just()
        {
            await AsyncEnumerable.Just(1)
                .DistinctUntilChanged()
                .AssertResult(1);
        }

        [Fact]
        public async void Range()
        {
            await AsyncEnumerable.Range(1, 5)
                .DistinctUntilChanged()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Redundant()
        {
            await AsyncEnumerable.FromArray(1, 2, 3, 3, 2, 1, 1, 4, 5, 4, 4)
                .DistinctUntilChanged()
                .AssertResult(1, 2, 3, 2, 1, 4, 5, 4);
        }

        [Fact]
        public async void Redundant_Comparer()
        {
            await AsyncEnumerable.FromArray(1, 2, 3, 3, 2, 1, 1, 4, 5, 4, 4)
                .DistinctUntilChanged(EqualityComparer<int>.Default)
                .AssertResult(1, 2, 3, 2, 1, 4, 5, 4);
        }

        [Fact]
        public async void KeySelector()
        {
            await AsyncEnumerable.Range(1, 10)
                .DistinctUntilChanged(k => k / 3)
                .AssertResult(1, 3, 6, 9);
        }

        [Fact]
        public async void KeySelector_KeyComparer()
        {
            await AsyncEnumerable.Range(1, 10)
                .DistinctUntilChanged(k => k / 3, EqualityComparer<long>.Default)
                .AssertResult(1, 3, 6, 9);
        }
    }
}
