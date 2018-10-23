using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class SkipUntilTest
    {
        [Fact]
        public async void Skip_All()
        {
            await AsyncEnumerable.Range(1, 5)
                .SkipUntil(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(250)))
                .AssertResult();
        }

        [Fact]
        public async void Skip_None()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMap(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(100)).Map(w => v))
                .SkipUntil(AsyncEnumerable.Empty<int>())
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Skip_Some()
        {
            int scale = 200;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                scale = 2000;
            }
            await AsyncEnumerable.Range(1, 5)
                .FlatMap(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(v * scale)).Map(w => v))
                .SkipUntil(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(5 * scale / 2)))
                .AssertResult(3, 4, 5);
        }
    }
}
