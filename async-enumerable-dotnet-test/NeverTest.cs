using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class NeverTest
    {
        /*
        [Fact(Skip = "The task of MoveNextAsync never completes and thus DisposeAsync won't either")]
        public async void Never()
        {
            await AsyncEnumerable.Never<int>()
                .Timeout(TimeSpan.FromMilliseconds(100))
                .AssertFailure(typeof(TimeoutException));
        }
        */

        [Fact]
        public void Normal()
        {
            var en = AsyncEnumerable.Never<int>().GetAsyncEnumerator();

            // no await as the test would never end otherwise
            en.MoveNextAsync();
            en.DisposeAsync();
        }
    }
}
