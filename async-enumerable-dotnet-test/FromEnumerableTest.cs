using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Linq;

namespace async_enumerable_dotnet_test
{
    public class FromEnumerableTest
    {
        [Fact]
        public async void Normal()
        {
            var result = AsyncEnumerable.FromEnumerable(Enumerable.Range(1, 5))
                ;

            await result.AssertResult(1, 2, 3, 4, 5);
        }
    }
}
