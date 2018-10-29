// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Collections.Generic;

namespace async_enumerable_dotnet_test
{
    public class ToListTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .ToList()
                .AssertResult(new List<int>(new[] { 1, 2, 3, 4, 5 }));
        }

        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .ToList()
                .AssertResult(new List<int>());
        }

        [Fact]
        public async void Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .ToList()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
