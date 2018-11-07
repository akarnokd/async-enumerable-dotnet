// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class ZipTest
    {
        [Fact]
        public async void SameLength()
        {
            var source = AsyncEnumerable.FromArray(1, 2, 3, 4);

            var result = AsyncEnumerable.Zip(v => v.Sum(), source, source, source);

            await result.AssertResult(3, 6, 9, 12);
        }

        [Fact]
        public async void DifferentLengths()
        {
            var source1 = AsyncEnumerable.FromArray(1, 2, 3, 4);
            var source2 = AsyncEnumerable.FromArray(1, 2, 3);
            var source3 = AsyncEnumerable.FromArray(1, 2, 3, 4, 5);

            var result = AsyncEnumerable.Zip(v => v.Sum(), source1, source2, source3);

            await result.AssertResult(3, 6, 9);
        }

        [Fact]
        public async void Error_One()
        {
            await AsyncEnumerable.Zip(v => v.Sum(), 
                    AsyncEnumerable.Error<int>(new InvalidOperationException()),
                    AsyncEnumerable.Range(1, 5)
                )
                .AssertFailure(typeof(InvalidOperationException));
        }
        
        [Fact]
        public async void Error_Both()
        {
            await AsyncEnumerable.Zip(v => v.Sum(), 
                    AsyncEnumerable.Error<int>(new InvalidOperationException()),
                    AsyncEnumerable.Error<int>(new InvalidOperationException())
                )
                .AssertFailure(typeof(AggregateException));
        }

    }
}
