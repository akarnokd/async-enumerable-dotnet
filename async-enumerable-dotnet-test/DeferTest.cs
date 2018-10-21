using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class DeferTest
    {
        [Fact]
        public async void Normal()
        {
            var count = 0;
            var result = AsyncEnumerable.Defer(() =>
            {
                count++;
                return AsyncEnumerable.FromArray(1, 2, 3, 4, 5);
            });

            Assert.Equal(0, count);

            await result.AssertResult(1, 2, 3, 4, 5);

            Assert.Equal(1, count);

            await result.AssertResult(1, 2, 3, 4, 5);

            Assert.Equal(2, count);
        }
    }
}
