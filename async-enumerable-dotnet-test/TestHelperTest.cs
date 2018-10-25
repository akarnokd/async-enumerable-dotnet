using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Collections.Generic;
using static async_enumerable_dotnet_test.GroupByTest;

namespace async_enumerable_dotnet_test
{
    public class TestHelperTest
    {
        [Fact]
        public async void AssertResultSet()
        {
            await AsyncEnumerable.Range(1, 3)
                .AssertResultSet(1, 2, 3);
        }

        [Fact]
        public async void AssertResultSet_List()
        {
            await AsyncEnumerable.FromArray(new List<int>(new [] { 1, 2, 3 }))
                .AssertResultSet(
                    ListComparer<int>.Default,
                    new List<int>(new[] { 1, 2, 3 }));
        }

        [Fact]
        public void HashSet_Contains()
        {
            var set = new HashSet<IList<int>>(ListComparer<int>.Default);

            set.Add(new List<int>(new[] { 1, 2, 3 }));

            Assert.Contains(new List<int>(new[] { 1, 2, 3 }), set);

            Assert.True(set.Remove(new List<int>(new[] { 1, 2, 3 })));
        }
    }
}
