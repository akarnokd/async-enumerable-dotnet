using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class FirstLastSingleAsyncTest
    {
        [Fact]
        public async void First()
        {
            Assert.Equal(1, await AsyncEnumerable.Range(1, 5).FirstAsync());
        }

        [Fact]
        public async void First_Empty()
        {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => AsyncEnumerable.Range(1, 0).FirstAsync().AsTask());
        }

        [Fact]
        public async void First_Or_Default()
        {
            Assert.Equal(10, await AsyncEnumerable.Range(1, 0).FirstAsync(10));
        }

        [Fact]
        public async void Last()
        {
            Assert.Equal(5, await AsyncEnumerable.Range(1, 5).LastAsync());
        }

        [Fact]
        public async void Last_Empty()
        {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => AsyncEnumerable.Range(1, 0).LastAsync().AsTask());
        }

        [Fact]
        public async void Last_Or_Default()
        {
            Assert.Equal(10, await AsyncEnumerable.Range(1, 0).LastAsync(10));
        }

        [Fact]
        public async void Single()
        {
            Assert.Equal(0, await AsyncEnumerable.Just(0).SingleAsync());
        }

        [Fact]
        public async void Single_Empty()
        {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => AsyncEnumerable.Empty<int>().SingleAsync().AsTask());
        }

        [Fact]
        public async void Single_Or_Default()
        {
            Assert.Equal(10, await AsyncEnumerable.Empty<int>().SingleAsync(10));
        }

        [Fact]
        public async void Single_Too_Many()
        {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => AsyncEnumerable.Range(1, 5).SingleAsync().AsTask());
        }
    }
}
