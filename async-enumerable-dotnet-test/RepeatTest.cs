using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class RepeatTest
    {
        [Fact]
        public async void Unlimited()
        {
            await AsyncEnumerable.Just(1)
                .Repeat()
                .Take(5)
                .AssertResult(1, 1, 1, 1, 1);
        }

        [Fact]
        public async void Limited()
        {
            await AsyncEnumerable.Range(1, 2)
                .Repeat(3)
                .AssertResult(1, 2, 1, 2, 1, 2);
        }

        [Fact]
        public async void Limited_Condition()
        {
            await AsyncEnumerable.Range(1, 2)
                .Repeat(n => n < 2)
                .AssertResult(1, 2, 1, 2, 1, 2);
        }

    }
}
