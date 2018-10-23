using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class ConsumeTest
    {
        [Fact]
        public async void Normal()
        {
            var push = new MulticastAsyncEnumerable<int>();

            var t1 = push.AssertResult(1, 2, 3, 4, 5);
            var t2 = push.AssertResult(1, 2, 3, 4, 5);

            await AsyncEnumerable.Range(1, 5)
                .Consume(push);

            await t1;
            await t2;
        }
    }
}
