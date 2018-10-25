using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class PrefetchTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 10)
                .Prefetch(2)
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void Normal_1()
        {
            await AsyncEnumerable.Range(1, 10)
                .Prefetch(1)
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void Error()
        {
            await AsyncEnumerable.Range(1, 10)
                .ConcatWith(AsyncEnumerable.Error<int>(new InvalidOperationException()))
                .Prefetch(2)
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void Fetch_More_Than_The_Length()
        {
            await AsyncEnumerable.Range(1, 10)
                .Prefetch(20)
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void Long_Fetch_1000()
        {
            await AsyncEnumerable.Range(1, 1_000_000)
                .Prefetch(1000)
                .Last()
                .AssertResult(1_000_000);
        }

        [Fact]
        public async void Long_Fetch_1000_Limit_500()
        {
            await AsyncEnumerable.Range(1, 1_000_000)
                .Prefetch(1000, 500)
                .Last()
                .AssertResult(1_000_000);
        }
    }
}
