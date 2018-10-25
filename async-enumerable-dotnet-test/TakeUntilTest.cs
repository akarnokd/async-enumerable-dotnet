using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Threading;

namespace async_enumerable_dotnet_test
{
    public class TakeUntilTest
    {
        [Fact]
        public async void Normal()
        {
            var t = 500;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 1500;
            }
            var disposedMain = 0;
            var disposedOther = 0;

            await AsyncEnumerable.Range(1, 5)
                .DoOnDispose(() => disposedMain++)
                .TakeUntil(
                    AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(t))
                    .DoOnDispose(() => disposedOther += 2)
                )
                .AssertResult(1, 2, 3, 4, 5);

            Assert.Equal(1, disposedMain);
            Assert.Equal(2, disposedOther);
        }

        [Fact]
        public async void Until()
        {
            var disposedMain = 0;
            var disposedOther = 0;

            await AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(500))
                .DoOnDispose(() => disposedMain++)
                .TakeUntil(
                    AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(100))
                    .DoOnDispose(() => disposedOther += 2)
                )
                .AssertResult();

            Assert.Equal(1, disposedMain);
            Assert.Equal(2, disposedOther);
        }
    }
}
