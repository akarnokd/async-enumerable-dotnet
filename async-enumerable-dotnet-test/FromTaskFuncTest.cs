using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class FromTaskFuncTest
    {
        [Fact]
        public async void Normal()
        {
            var source = AsyncEnumerable.FromTask(async () =>
            {
                await Task.Delay(100);
                return 1;
            });

            await source.AssertResult(1);
        }
    }
}
