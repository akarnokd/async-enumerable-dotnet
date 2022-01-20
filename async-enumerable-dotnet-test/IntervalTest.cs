// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class IntervalTest
    {
        [Fact]
        public async Task Normal()
        {
            await AsyncEnumerable.Interval(TimeSpan.FromMilliseconds(100))
                .Take(5)
                .AssertResult(0, 1, 2, 3, 4);
        }

        [Fact]
        public async Task Normal_initial()
        {
            await AsyncEnumerable.Interval(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100))
                .Take(5)
                .AssertResult(0, 1, 2, 3, 4);
        }

        [Fact]
        public async Task Range()
        {
            await AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100))
                .AssertResult(1, 2, 3, 4, 5);
        }
    }
}
