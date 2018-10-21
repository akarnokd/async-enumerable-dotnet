using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class JustTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Just(1)
                .AssertResult(1);
        }
    }
}
