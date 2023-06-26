// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class ErrorTest
    {
        [Fact]
        public async Task Calls()
        {
            var en = AsyncEnumerable.Error<int>(new InvalidOperationException()).GetAsyncEnumerator(default);

            try
            {
                await en.MoveNextAsync();
                Assert.False(true, "Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected;
            }
            
            Assert.Equal(0, en.Current);
            Assert.False(await en.MoveNextAsync());

            await en.DisposeAsync();
        }
    }
}
