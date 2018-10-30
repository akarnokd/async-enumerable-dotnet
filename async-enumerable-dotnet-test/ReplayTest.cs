// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System;

namespace async_enumerable_dotnet_test
{
    public class ReplayTest
    {
        [Fact]
        public async void All_Direct()
        {
            await AsyncEnumerable.Range(1, 5)
                .Replay(v => v)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void All_Simple()
        {
            await AsyncEnumerable.Range(1, 5)
                .Replay(v => v.Map(w => w + 1))
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Fact]
        public async void All_Take()
        {
            await AsyncEnumerable.Range(1, 5)
                .Replay(v => v.Take(3))
                .AssertResult(1, 2, 3);
        }

        [Fact]
        public async void All_Recombine()
        {
            await AsyncEnumerable.Range(1, 5)
                .Replay(v => v.Take(3).ConcatWith(v.Skip(3)))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void All_Twice()
        {
            await AsyncEnumerable.Range(1, 5)
                .Replay(v => v.ConcatWith(v))
                .AssertResult(1, 2, 3, 4, 5, 1, 2, 3, 4, 5);
        }

        [Fact]
        public async void All_Handler_Crash()
        {
            await AsyncEnumerable.Range(1, 5)
                .Replay<int, int>(v => throw new InvalidOperationException())
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
