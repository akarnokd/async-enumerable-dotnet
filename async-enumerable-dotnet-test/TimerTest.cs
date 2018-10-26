using System;
using System.Threading;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class TimerTest
    {
        [Fact]
        public async void Token()
        {
            var value = 0;
            
            var cts = new CancellationTokenSource();
#pragma warning disable 4014
            AsyncEnumerable.Timer(TimeSpan.FromSeconds(200), cts.Token)
                .DoOnNext(v => value = 1)
                .GetAsyncEnumerator()
                .MoveNextAsync();
#pragma warning restore 4014

            await Task.Delay(100);
            
            cts.Cancel();

            await Task.Delay(200);
            
            Assert.Equal(0, value);
        }
    }
}
