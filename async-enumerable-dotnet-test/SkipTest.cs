using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class SkipTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 10)
                .Skip(5)
                .AssertResult(6, 7, 8, 9, 10);
        }

        [Fact]
        public async void Zero()
        {
            await AsyncEnumerable.Range(1, 10)
                .Skip(0)
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void All()
        {
            await AsyncEnumerable.Range(1, 10)
                .Skip(10)
                .AssertResult();
        }
    }
}
