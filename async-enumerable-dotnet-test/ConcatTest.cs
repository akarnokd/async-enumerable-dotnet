using System;
using System.Collections.Generic;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

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
    }
}
