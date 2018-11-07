// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class DebounceTest
    {
        [Fact]
        public async void Keep_All()
        {
            var t = 100;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 1000;
            }

            await AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(2 * t))
                .Debounce(TimeSpan.FromMilliseconds(t))
                .AssertResult(1, 2, 3, 4);
        }

        [Fact]
        public async void Skip_All()
        {
            await AsyncEnumerable.Range(1, 5)
                .Debounce(TimeSpan.FromMilliseconds(1000))
                .AssertResult();
        }

        [Fact]
        public async void Keep_All_EmitLast()
        {
            var t = 100;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 1000;
            }

            await AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(2 * t))
                .Debounce(TimeSpan.FromMilliseconds(t), true)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Skip_All_EmitLast()
        {
            await AsyncEnumerable.Range(1, 5)
                .Debounce(TimeSpan.FromMilliseconds(1000), true)
                .AssertResult(5);
        }

        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Debounce(TimeSpan.FromMilliseconds(1000))
                .AssertResult();
        }

        [Fact]
        public async void Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .Debounce(TimeSpan.FromMilliseconds(1000))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Error_EmitLast()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .Debounce(TimeSpan.FromMilliseconds(1000), true)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Item_Error_EmitLast()
        {
            await AsyncEnumerable.Just(1).WithError(new InvalidOperationException())
                .Debounce(TimeSpan.FromMilliseconds(1000), true)
                .AssertFailure(typeof(InvalidOperationException), 1);
        }

        [Fact]
        public async void Delayed_Completion_After_Debounced_Item()
        {
            await AsyncEnumerable.Just(1)
                .ConcatWith(
                    AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200))
                    .Map(v => 0)
                    .IgnoreElements()
                )
                .Debounce(TimeSpan.FromMilliseconds(100))
                .AssertResult(1);
        }

        [Fact]
        public async void Long_Source_Skipped()
        {
            await AsyncEnumerable.Range(1, 1_000_000)
                .Debounce(TimeSpan.FromSeconds(10))
                .AssertResult();
        }

        [Fact]
        public async void Long_Source_Skipped_EmitLast()
        {
            await AsyncEnumerable.Range(1, 1_000_000)
                .Debounce(TimeSpan.FromSeconds(10), true)
                .AssertResult(1_000_000);
        }

        [Fact]
        public async void Take()
        {
            await AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(200))
                .Debounce(TimeSpan.FromMilliseconds(100))
                .Take(1)
                .AssertResult(1L);
        }
        
        [Fact]
        public async void Take_EmitLatest()
        {
            await AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(200))
                .Debounce(TimeSpan.FromMilliseconds(100), true)
                .Take(1)
                .AssertResult(1L);
        }
    }
}
