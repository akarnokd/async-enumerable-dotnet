// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class DefaultIfEmptyTest
    {
        [Fact]
        public async void NonEmpty()
        {
            await AsyncEnumerable.Range(1, 5)
                .DefaultIfEmpty(100)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .DefaultIfEmpty(100)
                .AssertResult(100);
        }
    }
}
