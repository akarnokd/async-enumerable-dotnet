// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class DistinctTest
    {
        [Fact]
        public async Task Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Distinct()
                .AssertResult();
        }

        [Fact]
        public async Task Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .Distinct()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task Redundant()
        {
            await AsyncEnumerable.FromArray(1, 2, 3, 2, 1, 4, 5, 1, 5)
                .Distinct()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task KeySelector()
        {
            await AsyncEnumerable.Range(1, 5)
                .Distinct(v => v % 3)
                .AssertResult(1, 2, 3);
        }

        [Fact]
        public async Task EqualityComparer()
        {
            await AsyncEnumerable.Range(1, 5)
                .Distinct(EqualityComparer<int>.Default)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task KeySelector_EqualityComparer()
        {
            await AsyncEnumerable.Range(1, 5)
                .Distinct(v => v % 3, EqualityComparer<long>.Default)
                .AssertResult(1, 2, 3);
        }

        [Fact]
        public async Task Custom_Set()
        {
            await AsyncEnumerable.Range(1, 5)
                .Distinct(v => v % 3, () => new HashSet<long>())
                .AssertResult(1, 2, 3);
        }
        
        [Fact]
        public async Task Custom_Crash()
        {
            await AsyncEnumerable.Range(1, 5)
                .Distinct(v => v % 3, () => throw new InvalidOperationException())
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
