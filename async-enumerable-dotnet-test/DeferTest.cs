// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class DeferTest
    {
        [Fact]
        public async void Normal()
        {
            var count = 0;
            var result = AsyncEnumerable.Defer(() =>
            {
                count++;
                return AsyncEnumerable.FromArray(1, 2, 3, 4, 5);
            });

            Assert.Equal(0, count);

            await result.AssertResult(1, 2, 3, 4, 5);

            Assert.Equal(1, count);

            await result.AssertResult(1, 2, 3, 4, 5);

            Assert.Equal(2, count);
        }

        [Fact]
        public async void Crash()
        {
            await AsyncEnumerable.Defer<int>(() => throw new InvalidOperationException())
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
