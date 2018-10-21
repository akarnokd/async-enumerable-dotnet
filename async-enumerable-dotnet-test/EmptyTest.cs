using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class EmptyTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Empty<int>()
                .AssertResult();
        }
    }
}
