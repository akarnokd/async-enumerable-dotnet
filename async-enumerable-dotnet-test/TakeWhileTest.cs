using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class TakeWhileTest
    {
        [Fact]
        public async void All_Pass()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeWhile(v => true)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Some_Pass()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeWhile(v => v < 4)
                .AssertResult(1, 2, 3);
        }

        [Fact]
        public async void None_Pass()
        {
            await AsyncEnumerable.Range(1, 5)
                .TakeWhile(v => false)
                .AssertResult();
        }
    }
}
