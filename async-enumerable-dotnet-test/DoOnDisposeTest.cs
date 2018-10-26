using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class DoOnDisposeTest
    {
        [Fact]
        public async void Sync_Normal()
        {
            var count = 0;
            await AsyncEnumerable.Range(1, 5)
                .DoOnDispose(() => count++)
                .AssertResult(1, 2, 3, 4, 5);
            
            Assert.Equal(1, count);
        }
        
        [Fact]
        public async void Async_Normal()
        {
            var count = 0;
            await AsyncEnumerable.Range(1, 5)
                .DoOnDispose(async () =>
                {
                    await Task.Delay(100);
                    count++;
                })
                .AssertResult(1, 2, 3, 4, 5);
            
            Assert.Equal(1, count);
        }

    }
}
