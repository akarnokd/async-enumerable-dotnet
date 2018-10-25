using System.Collections.Generic;
using System.Threading.Tasks;

using async_enumerable_dotnet;

using Xunit;

namespace async_enumerable_dotnet_test
{
    public class ToCollectionTest
    {
        [Fact]
        public async Task ToListAllowsEmptySource()
        {
            var result = await AsyncEnumerable.FromArray(new int[0]).ToList();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ToListAlwaysReturnsNewInstance()
        {
            var source = AsyncEnumerable.FromArray(new int[0]);
            var result1 = await source.ToList();
            var result2 = await source.ToList();
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public async Task ToListReturnsAllItems()
        {
            var result = await AsyncEnumerable.Range(0, 10).ToList();
            var usedNumbers = new HashSet<int>(result);
            Assert.Equal(10, result.Count);
            for (var i = 0; i != 10; ++i)
            {
                Assert.Contains(i, usedNumbers);
            }
        }

        [Fact]
        public async Task ToArrayAllowsEmptySource()
        {
            var result = await AsyncEnumerable.FromArray(new int[0]).ToArray();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ToArrayAlwaysReturnsNewInstanceForEmptySource()
        {
            var source = AsyncEnumerable.FromArray(new int[0]);
            var result1 = await source.ToArray();
            var result2 = await source.ToArray();
            Assert.Same(result1, result2);
        }

        [Fact]
        public async Task ToArrayReturnsAllItems()
        {
            var result = await AsyncEnumerable.Range(0, 10).ToArray();
            var usedNumbers = new HashSet<int>(result);
            Assert.Equal(10, result.Length);
            for (var i = 0; i != 10; ++i)
            {
                Assert.Contains(i, usedNumbers);
            }
        }
    }
}
