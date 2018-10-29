// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class SkipLastTest
    {
        [Fact]
        public async void Skip_None()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipLast(0)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Skip_Some()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipLast(2)
                .AssertResult(1, 2, 3);
        }

        [Fact]
        public async void Skip_All()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipLast(5)
                .AssertResult();
        }

        [Fact]
        public async void Skip_More()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipLast(10)
                .AssertResult();
        }
    }
}
