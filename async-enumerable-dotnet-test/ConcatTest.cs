// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class ConcatTest
    {
        [Fact]
        public async void Array_Normal()
        {
            await AsyncEnumerable.Concat(
                    AsyncEnumerable.Range(1, 3),
                    AsyncEnumerable.Empty<int>(),
                    AsyncEnumerable.FromArray(4, 5, 6, 7),
                    AsyncEnumerable.Empty<int>(),
                    AsyncEnumerable.Just(8),
                    AsyncEnumerable.FromEnumerable(new[] { 9, 10 }),
                    AsyncEnumerable.Empty<int>()
                )
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void Enumerable_Normal()
        {
            await AsyncEnumerable.Concat((IEnumerable<IAsyncEnumerable<int>>)new[] {
                        AsyncEnumerable.Range(1, 3),
                        AsyncEnumerable.Empty<int>(),
                        AsyncEnumerable.FromArray(4, 5, 6, 7),
                        AsyncEnumerable.Empty<int>(),
                        AsyncEnumerable.Just(8),
                        AsyncEnumerable.FromEnumerable(new[] { 9, 10 }),
                        AsyncEnumerable.Empty<int>()
                    }
                )
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void ConcatWith_Normal()
        {
            await AsyncEnumerable.Range(1, 3)
                .ConcatWith(AsyncEnumerable.Range(4, 2))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void ConcatWith_Take()
        {
            await AsyncEnumerable.Range(1, 5)
                .Take(3)
                .ConcatWith(AsyncEnumerable.Range(4, 2))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Async_Normal()
        {
            await 
                AsyncEnumerable.FromArray(
                    AsyncEnumerable.Range(1, 3),
                    AsyncEnumerable.Empty<int>(),
                    AsyncEnumerable.FromArray(4, 5, 6, 7),
                    AsyncEnumerable.Empty<int>(),
                    AsyncEnumerable.Just(8),
                    AsyncEnumerable.FromEnumerable(new[] { 9, 10 }),
                    AsyncEnumerable.Empty<int>()
                )
                .Concat()
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }
    }
}
