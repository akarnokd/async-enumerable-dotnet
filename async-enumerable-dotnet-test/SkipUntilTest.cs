// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class SkipUntilTest
    {
        [Fact]
        public async void Skip_All()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipUntil(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(250)))
                .AssertResult();
        }

        [Fact]
        public async void Skip_None()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMap(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(100)).Map(w => v))
                .SkipUntil(AsyncEnumerable.Empty<int>())
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Skip_Some()
        {
            var scale = 200;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                scale = 2000;
            }
            await AsyncEnumerable.Range(1, 5)
                .FlatMap(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(v * scale)).Map(w => v))
                // ReSharper disable once PossibleLossOfFraction
                .SkipUntil(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(5 * scale / 2)))
                .AssertResult(3, 4, 5);
        }

        [Fact]
        public async void Error_Main()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .SkipUntil(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200)))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Error_Other()
        {
            await AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200))
                .SkipUntil(AsyncEnumerable.Error<int>(new InvalidOperationException()))
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
