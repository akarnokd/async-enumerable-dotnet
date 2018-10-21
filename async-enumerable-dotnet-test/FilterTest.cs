using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class FilterTest
    {
        [Fact]
        public async void Normal()
        {
            var result = AsyncEnumerable.Range(1, 10)
                .Filter(v => v % 2 == 0);

            await result.AssertResult(2, 4, 6, 8, 10);
        }
    }
}
