// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using async_enumerable_dotnet;
using System;
using Xunit;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class TimeoutTest
    {
        [Fact]
        public async Task NoTimeout()
        {
            var result = AsyncEnumerable.FromArray(1, 2, 3, 4, 5)
                .Timeout(TimeSpan.FromMilliseconds(200))
                ;

            await result.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task HasTimeout()
        {
            var disposed = 0;

            var result = AsyncEnumerable.Timer(TimeSpan.FromSeconds(1))
                .DoOnDispose(() => disposed++)
                .Timeout(TimeSpan.FromMilliseconds(100))
                ;

            await result.AssertFailure(typeof(TimeoutException));

            Assert.Equal(1, disposed);
        }

        [Fact]
        public async Task Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .Timeout(TimeSpan.FromMilliseconds(10000))
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
