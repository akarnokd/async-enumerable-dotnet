// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class UsingTest
    {
        [Fact]
        public async void Normal()
        {
            var cleanup = 0;

            var result = AsyncEnumerable.Using(() => 1,
                v => AsyncEnumerable.FromArray(v),
                v => cleanup = v);

            await result.AssertResult(1);

            Assert.Equal(1, cleanup);
        }
    }
}
