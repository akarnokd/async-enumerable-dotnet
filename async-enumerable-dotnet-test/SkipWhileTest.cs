// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class SkipWhileTest
    {
        [Fact]
        public async Task Skip_All()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipWhile(v => v < 6)
                .AssertResult();
        }

        [Fact]
        public async Task Skip_Some()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipWhile(v => v < 3)
                .AssertResult(3, 4, 5);
        }

        [Fact]
        public async Task Skip_None()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipWhile(v => v < 1)
                .AssertResult(1, 2, 3, 4, 5);
        }
    }
}
