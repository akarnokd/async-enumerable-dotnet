using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class LatestTest
    {
        [Fact]
        public async void Skip_All()
        {
            var en = AsyncEnumerable.Range(1, 5).Latest().GetAsyncEnumerator();

            try
            {
                await Task.Delay(200);

                Assert.True(await en.MoveNextAsync());

                Assert.Equal(5, en.Current);

                Assert.False(await en.MoveNextAsync());
            }
            finally
            {
                await en.DisposeAsync();
            }
        }

        [Fact]
        public async void Normal()
        {
            var push = new MulticastAsyncEnumerable<int>();
            var en = push.Latest().GetAsyncEnumerator();

            try
            {
                await push.Next(1);

                Assert.True(await en.MoveNextAsync());

                Assert.Equal(1, en.Current);

                await push.Next(2);
                await push.Next(3);

                await Task.Delay(200);

                Assert.True(await en.MoveNextAsync());

                Assert.Equal(3, en.Current);

                await push.Next(4);
                await push.Complete();

                Assert.True(await en.MoveNextAsync());

                Assert.Equal(4, en.Current);
                Assert.False(await en.MoveNextAsync());
            }
            finally
            {
                await en.DisposeAsync();
            }
        }
    }
}
