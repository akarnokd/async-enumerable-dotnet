using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class OnErrorResumeNextTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .OnErrorResumeNext(e => AsyncEnumerable.Range(6, 5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Error_Switch()
        {
            await AsyncEnumerable.Error<int>(new Exception())
                .OnErrorResumeNext(e => AsyncEnumerable.Range(6, 5))
                .AssertResult(6, 7, 8, 9, 10);
        }
    }
}
