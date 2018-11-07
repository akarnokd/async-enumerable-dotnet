// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class CountTest
    {
        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Count()
                .AssertResult(0L);
        }
        
        [Fact]
        public async void Just()
        {
            await AsyncEnumerable.Just(1)
                .Count()
                .AssertResult(1L);
        }

        [Fact]
        public async void Range()
        {
            await AsyncEnumerable.Range(1, 100)
                .Count()
                .AssertResult(100L);
        }

        
        [Fact]
        public async void Error()
        {
            await AsyncEnumerable.Range(1, 100).WithError(new InvalidOperationException())
                .Count()
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
