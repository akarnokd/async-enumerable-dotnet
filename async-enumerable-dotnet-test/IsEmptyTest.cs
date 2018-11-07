// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class IsEmptyTest
    {
        [Fact]
        public async void NonEmpty()
        {
            await AsyncEnumerable.Range(1, 5)
                .IsEmpty()
                .AssertResult(false);
        }
        
        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .IsEmpty()
                .AssertResult(true);
        }

        [Fact]
        public async void Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .IsEmpty()
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
