// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

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
