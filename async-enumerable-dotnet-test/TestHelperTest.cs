// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class TestHelperTest
    {
        [Fact]
        public async Task AssertResultSet()
        {
            await AsyncEnumerable.Range(1, 3)
                .AssertResultSet(1, 2, 3);
        }

        [Fact]
        public async Task AssertResultSet_List()
        {
            await AsyncEnumerable.FromArray(new List<int>(new [] { 1, 2, 3 }))
                .AssertResultSet(
                    ListComparer<int>.Default,
                    new List<int>(new[] { 1, 2, 3 }));
        }

        [Fact]
        public void HashSet_Contains()
        {
            var set = new HashSet<IList<int>>(ListComparer<int>.Default) {new List<int>(new[] {1, 2, 3})};


            Assert.Contains(new List<int>(new[] { 1, 2, 3 }), set);

            Assert.True(set.Remove(new List<int>(new[] { 1, 2, 3 })));
        }
    }
}
