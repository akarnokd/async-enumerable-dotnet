using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

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
