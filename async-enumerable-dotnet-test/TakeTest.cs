// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class TakeTest
    {
        [Fact]
        public async void Normal()
        {
            var result = AsyncEnumerable.FromArray(1, 2, 3, 4, 5)
                .Take(3)
                ;

            await result.AssertResult(1, 2, 3);
        }

        [Fact]
        public async void More()
        {
            var result = AsyncEnumerable.FromArray(1, 2, 3, 4, 5)
                .Take(6)
                ;

            await result.AssertResult(1, 2, 3, 4, 5);
        }
    }
}
