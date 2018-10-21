using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class TakeTest
    {
        [Fact]
        public async void Normal()
        {
            var result = AsyncEnumerable.FromArray(1, 2, 3, 4, 5)
                .Take(3)
                ;

            await result.AssertResult(1, 2, 3);
        }

        [Fact]
        public async void More()
        {
            var result = AsyncEnumerable.FromArray(1, 2, 3, 4, 5)
                .Take(6)
                ;

            await result.AssertResult(1, 2, 3, 4, 5);
        }
    }
}
