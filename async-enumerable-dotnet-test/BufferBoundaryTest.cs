// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class BufferBoundaryTest
    {
        [Fact]
        public async Task Size()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200)), 2)
                .AssertResult(
                    new List<int>(new [] { 1, 2 }),
                    new List<int>(new[] { 3, 4 }),
                    new List<int>(new[] { 5 })
                );
        }

        [Fact]
        public async Task Size_Collection()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200)), () => new HashSet<int>(), 2)
                .AssertResult(
                    new HashSet<int>(new [] { 1, 2 }),
                    new HashSet<int>(new[] { 3, 4 }),
                    new HashSet<int>(new[] { 5 })
                );
        }

        [Fact]
        public async Task Time()
        {
            var t = 100L;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 1000L;
            }

            await
                TestHelper.TimeSequence(
                    t, t,
                    3 * t, 3 * t, 3 * t,
                    7 * t, 7 * t
                )
                .Buffer(AsyncEnumerable.Interval(TimeSpan.FromMilliseconds(2 * t)))
                .AssertResult(
                    new List<long>(new[] { t, t }),
                    new List<long>(new[] { 3 * t, 3 * t, 3 * t }),
                    new List<long>(),
                    new List<long>(new[] { 7 * t, 7 * t })
                );

        }

        [Fact]
        public async Task Error_Main()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .Buffer(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200)), 2)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async Task Error_Other()
        {
            await AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200))
                .Buffer(AsyncEnumerable.Error<int>(new InvalidOperationException()), 2)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async Task Empty_Main()
        {
            await AsyncEnumerable.Empty<int>()
                .Buffer(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200)), 2)
                .AssertResult();
        }

        [Fact]
        public async Task Empty_Other()
        {
            await AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200))
                .Buffer(AsyncEnumerable.Empty<int>(), 2)
                .AssertResult();
        }

        [Fact]
        public async Task Time_And_Size()
        {
            var t = 100L;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 1000L;
            }

            await
                TestHelper.TimeSequence(
                    t, t,
                    3 * t, 3 * t, 3 * t,
                    7 * t, 7 * t, 7 * t, 7 * t
                )
                .Buffer(AsyncEnumerable.Interval(TimeSpan.FromMilliseconds(2 * t)), 3)
                .AssertResult(
                    new List<long>(new[] { t, t }),
                    new List<long>(new[] { 3 * t, 3 * t, 3 * t }),
                    new List<long>(),
                    new List<long>(),
                    new List<long>(new[] { 7 * t, 7 * t, 7 * t }),
                    new List<long>(new[] { 7 * t })
                );

        }
    }
}
