using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class FirstLastSingleTaskTest
    {
        [Fact]
        public async void First()
        {
            Assert.Equal(1, await AsyncEnumerable.Range(1, 5).FirstTask());
        }

        [Fact]
        public async void First_Empty()
        {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => AsyncEnumerable.Range(1, 0).FirstTask().AsTask());
        }

        [Fact]
        public async void First_Or_Default()
        {
            Assert.Equal(10, await AsyncEnumerable.Range(1, 0).FirstTask(10));
        }

        [Fact]
        public async void Last()
        {
            Assert.Equal(5, await AsyncEnumerable.Range(1, 5).LastTask());
        }

        [Fact]
        public async void Last_Empty()
        {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => AsyncEnumerable.Range(1, 0).LastTask().AsTask());
        }

        [Fact]
        public async void Last_Or_Default()
        {
            Assert.Equal(10, await AsyncEnumerable.Range(1, 0).LastTask(10));
        }

        [Fact]
        public async void Single()
        {
            Assert.Equal(0, await AsyncEnumerable.Just(0).SingleTask());
        }

        [Fact]
        public async void Single_Empty()
        {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => AsyncEnumerable.Empty<int>().SingleTask().AsTask());
        }

        [Fact]
        public async void Single_Or_Default()
        {
            Assert.Equal(10, await AsyncEnumerable.Empty<int>().SingleTask(10));
        }

        [Fact]
        public async void Single_Too_Many()
        {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => AsyncEnumerable.Range(1, 5).SingleTask().AsTask());
        }
    }
}
