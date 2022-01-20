// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class PrefetchTest
    {
        [Fact]
        public async Task Normal()
        {
            await AsyncEnumerable.Range(1, 10)
                .Prefetch(2)
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async Task Normal_1()
        {
            await AsyncEnumerable.Range(1, 10)
                .Prefetch(1)
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async Task Error()
        {
            await AsyncEnumerable.Range(1, 10)
                .WithError(new InvalidOperationException())
                .Prefetch(2)
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async Task Fetch_More_Than_The_Length()
        {
            await AsyncEnumerable.Range(1, 10)
                .Prefetch(20)
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async Task Long_Fetch_1000()
        {
            await AsyncEnumerable.Range(1, 1_000_000)
                .Prefetch(1000)
                .Last()
                .AssertResult(1_000_000);
        }

        [Fact]
        public async Task Long_Fetch_1000_Limit_500()
        {
            await AsyncEnumerable.Range(1, 1_000_000)
                .Prefetch(1000, 500)
                .Last()
                .AssertResult(1_000_000);
        }

        [Fact]
        public async Task Take()
        {
            await AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(200))
                .Prefetch(128)
                .Take(1)
                .AssertResult(1L);
        }

        [Fact]
        public async Task Cancel()
        {
            await AsyncEnumerable.FromTask(Task.FromCanceled<int>(new CancellationToken(true)))
                .Prefetch(1)
                .AssertFailure(typeof(OperationCanceledException));
        }
    }
}
