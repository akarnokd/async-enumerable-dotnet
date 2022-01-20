// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class EmptyTest
    {
        [Fact]
        public async Task Normal()
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
