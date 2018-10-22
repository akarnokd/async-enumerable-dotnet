using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class IntervalTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Interval(TimeSpan.FromMilliseconds(100))
                .Take(5)
                .AssertResult(0, 1, 2, 3, 4);
        }

        [Fact]
        public async void Range()
        {
            await AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100))
                .AssertResult(1, 2, 3, 4, 5);
        }
    }
}
