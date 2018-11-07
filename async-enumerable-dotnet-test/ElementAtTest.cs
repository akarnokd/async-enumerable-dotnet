// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class ElementAtTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .ElementAt(2)
                .AssertResult(3);
        }
        
        [Fact]
        public async void Normal_Default()
        {
            await AsyncEnumerable.Range(1, 5)
                .ElementAt(2, 100)
                .AssertResult(3);
        }

        [Fact]
        public async void Missing()
        {
            await AsyncEnumerable.Range(1, 5)
                .ElementAt(10)
                .AssertFailure(typeof(IndexOutOfRangeException));
        }
        
        [Fact]
        public async void Missing_Default()
        {
            await AsyncEnumerable.Range(1, 5)
                .ElementAt(10, 100)
                .AssertResult(100);
        }
    }
}
