// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System;

namespace async_enumerable_dotnet_test
{
    public class WithLatestFromTest
    {
        [Fact]
        public async void Simple()
        {
            var t = 200;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 1000;
            }

            await AsyncEnumerable.Range(1, 5)
                .DoOnNext(async v =>
                {
                    if (v == 1)
                    {
                        // otherwise the first value may come too early.
                        await Task.Delay(t);
                    }
                })
                .WithLatestFrom(AsyncEnumerable.Just(10), (a, b) => a + b)
                .AssertResult(11, 12, 13, 14, 15);
        }

        [Fact]
        public async void Empty_Main()
        {
            await AsyncEnumerable.Empty<int>()
                .WithLatestFrom(AsyncEnumerable.Just(10), (a, b) => a + b)
                .AssertResult();
        }


        [Fact]
        public async void Error_Main()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .WithLatestFrom(AsyncEnumerable.Just(10), (a, b) => a + b)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Empty_Other()
        {
            await AsyncEnumerable.Range(1, 5)
                .WithLatestFrom(AsyncEnumerable.Empty<int>(), (a, b) => a + b)
                .AssertResult();
        }

        [Fact]
        public async void Error_Other()
        {
            var t = 200;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 1000;
            }

            await AsyncEnumerable.Just(1)
                .DoOnNext(async v =>
                {
                    if (v == 1)
                    {
                        // otherwise the first value may come too early.
                        await Task.Delay(t);
                    }
                })
                .WithLatestFrom(AsyncEnumerable.Error<int>(new InvalidOperationException()), (a, b) => a + b)
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
