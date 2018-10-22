using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

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
