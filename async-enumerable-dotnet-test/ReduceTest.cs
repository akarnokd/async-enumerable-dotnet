using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class ReduceTest
    {
        [Fact]
        public async void NoSeed_Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .Reduce((a, b) => a + b)
                .AssertResult(15);
        }

        [Fact]
        public async void NoSeed_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Reduce((a, b) => a + b)
                .AssertResult();
        }

        [Fact]
        public async void WithSeed_Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .Reduce(() => 10, (a, b) => a + b)
                .AssertResult(25);
        }

        [Fact]
        public async void WithSeed_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Reduce(() => 10, (a, b) => a + b)
                .AssertResult(10);
        }
    }
}
