// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class RangeTest
    {
        [Fact]
        public async void Normal()
        {
            var result = AsyncEnumerable.Range(1, 5);

            await result.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Empty()
        {
            var result = AsyncEnumerable.Range(1, 0);

            await result.AssertResult();
        }
    }
}
