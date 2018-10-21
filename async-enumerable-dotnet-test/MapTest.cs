using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class MapTest
    {
        [Fact]
        public async void Normal()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .Map(v => v * v);

            await result.AssertResult(1, 4, 9, 16, 25);
        }
    }
}
