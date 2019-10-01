// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class EmptyTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Empty<int>()
                .AssertResult();
        }

        [Fact]
        public void Current()
        {
            Assert.Equal(0, AsyncEnumerable.Empty<int>().GetAsyncEnumerator(default).Current);
        }
    }
}
