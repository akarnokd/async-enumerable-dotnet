// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class IgnoreElementsTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .IgnoreElements()
                .AssertResult();
        }

        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .IgnoreElements()
                .AssertResult();
        }

        [Fact]
        public void Current()
        {
            Assert.Equal(0, AsyncEnumerable.Range(1, 5)
                .IgnoreElements().GetAsyncEnumerator(default).Current);
        }
    }
}
