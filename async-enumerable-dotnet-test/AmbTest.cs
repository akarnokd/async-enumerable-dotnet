// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading;

namespace async_enumerable_dotnet_test
{
    public class AmbTest
    {
        [Fact]
        public async void First_Win()
        {
            await AsyncEnumerable.Amb(
                    AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(100)),
                    AsyncEnumerable.Interval(11, 5, TimeSpan.FromMilliseconds(200))
                )
                .AssertResult(1L, 2L, 3L, 4L, 5L);
        }

        [Fact]
        public async void First_Win_Empty()
        {
            await AsyncEnumerable.Amb(
                    AsyncEnumerable.Empty<long>(),
                    AsyncEnumerable.Interval(11, 5, TimeSpan.FromMilliseconds(200))
                )
                .AssertResult();
        }

        [Fact]
        public async void Second_Win()
        {
            await AsyncEnumerable.Amb(
                    AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(200)),
                    AsyncEnumerable.Interval(11, 5, TimeSpan.FromMilliseconds(100))
                )
                .AssertResult(11L, 12L, 13L, 14L, 15L);
        }

        [Fact]
        public async void Second_Win_Empty()
        {
            await AsyncEnumerable.Amb(
                    AsyncEnumerable.Interval(11, 5, TimeSpan.FromMilliseconds(100)),
                    AsyncEnumerable.Empty<long>()
                )
                .AssertResult();
        }

        [Fact]
        public async void Any_Win()
        {
            var disposed = 0;
            await AsyncEnumerable.Amb(
                    AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(100)).DoOnDispose(() => Interlocked.Increment(ref disposed)),
                    AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(100)).DoOnDispose(() => Interlocked.Add(ref disposed, 128))
                )
                .AssertResult(1L, 2L, 3L, 4L, 5L);

            Assert.Equal(129, disposed);
        }
    }
}
