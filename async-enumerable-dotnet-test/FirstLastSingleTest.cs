using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class FirstLastSingleTest
    {
        [Fact]
        public async void First_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .First()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Fact]
        public async void First_Empty_Default()
        {
            await AsyncEnumerable.Empty<int>()
                .First(1)
                .AssertResult(1);
        }

        [Fact]
        public async void First()
        {
            await AsyncEnumerable.Range(1, 5)
                .First()
                .AssertResult(1);
        }

        [Fact]
        public async void First_Default()
        {
            await AsyncEnumerable.Range(1, 5)
                .First(100)
                .AssertResult(1);
        }

        [Fact]
        public async void Last_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Last()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Fact]
        public async void Last_Empty_Default()
        {
            await AsyncEnumerable.Empty<int>()
                .Last(1)
                .AssertResult(1);
        }

        [Fact]
        public async void Last()
        {
            await AsyncEnumerable.Range(1, 5)
                .Last()
                .AssertResult(5);
        }

        [Fact]
        public async void Last_Default()
        {
            await AsyncEnumerable.Range(1, 5)
                .Last(100)
                .AssertResult(5);
        }

        [Fact]
        public async void Single_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Single()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Fact]
        public async void Single_Empty_Default()
        {
            await AsyncEnumerable.Empty<int>()
                .Single(1)
                .AssertResult(1);
        }

        [Fact]
        public async void Single()
        {
            await AsyncEnumerable.Just(1)
                .Single()
                .AssertResult(1);
        }

        [Fact]
        public async void Single_Default()
        {
            await AsyncEnumerable.Just(1)
                .Single(100)
                .AssertResult(1);
        }

        [Fact]
        public async void Single_More()
        {
            await AsyncEnumerable.Range(1, 5)
                .Single()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Fact]
        public async void Single_More_Default()
        {
            await AsyncEnumerable.Range(1, 5)
                .Single(100)
                .AssertFailure(typeof(IndexOutOfRangeException));
        }
    }
}
