// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class IsEmptyTest
    {
        [Fact]
        public async Task NonEmpty()
        {
            await AsyncEnumerable.Range(1, 5)
                .IsEmpty()
                .AssertResult(false);
        }
        
        [Fact]
        public async Task Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .IsEmpty()
                .AssertResult(true);
        }

        [Fact]
        public async Task Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .IsEmpty()
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
