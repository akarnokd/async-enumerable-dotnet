// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class TakeLastTest
    {
        [Fact]
        public async Task More()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeLast(10)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task All()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeLast(5)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task Some()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeLast(2)
                .AssertResult(4, 5);
        }

        [Fact]
        public async Task None()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeLast(0)
                .AssertResult();
        }
    }
}
