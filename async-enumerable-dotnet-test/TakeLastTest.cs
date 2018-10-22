using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class TakeLastTest
    {
        [Fact]
        public async void More()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeLast(10)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void All()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeLast(5)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Some()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeLast(2)
                .AssertResult(4, 5);
        }

        [Fact]
        public async void None()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeLast(0)
                .AssertResult();
        }
    }
}
