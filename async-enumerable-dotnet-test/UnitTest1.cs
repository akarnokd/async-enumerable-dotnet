using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
        {
            await Task.CompletedTask;
        }
    }
}
