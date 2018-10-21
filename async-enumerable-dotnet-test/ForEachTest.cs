using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class ForEachTest
    {
        [Fact]
        public async void Normal()
        {
            var sum = 0;
            await AsyncEnumerable.Range(1, 5)
                .ForEach(v => sum += v, onComplete: () => sum += 100);

            Assert.Equal(115, sum);
        }
    }
}
