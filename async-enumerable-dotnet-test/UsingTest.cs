// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class UsingTest
    {
        [Fact]
        public async Task Normal()
        {
            var cleanup = 0;

            var result = AsyncEnumerable.Using(() => 1,
                v => AsyncEnumerable.FromArray(v),
                v => cleanup = v);

            await result.AssertResult(1);

            Assert.Equal(1, cleanup);
        }

        [Fact]
        public async Task ResourceSupplier_Crash()
        {
            await AsyncEnumerable.Using<int, int>(() => throw new InvalidOperationException(),
                    v => AsyncEnumerable.Range(v, 5), v => { })
                .AssertFailure(typeof(InvalidOperationException));
        }
        
        [Fact]
        public async Task SourceSupplier_Crash()
        {
            await AsyncEnumerable.Using<int, int>(() => 1,
                    v => throw new InvalidOperationException(), v => { })
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async Task SourceSupplier_And_Cleanup_Crash()
        {
            await AsyncEnumerable.Using<int, int>(() => 1,
                    v => throw new InvalidOperationException(), v => throw new IndexOutOfRangeException())
                .AssertFailure(typeof(AggregateException));
        }

        [Fact]
        public async Task Cleanup_Crash()
        {
            try
            {
                await AsyncEnumerable.Using(() => 1,
                        v => AsyncEnumerable.Range(v, 5), v => throw new IndexOutOfRangeException())
                    .AssertResult(1, 2, 3, 4, 5);
                Assert.False(true, "Should have thrown");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }
        }
        
        [Fact]
        public async Task Upstream_Dispose_Crash()
        {
            try
            {
                await AsyncEnumerable.Using(() => 1,
                        v => AsyncEnumerable.Range(v, 5).DoOnDispose(() => throw new InvalidOperationException()),
                        v => { })
                    .AssertResult(1, 2, 3, 4, 5);
                Assert.False(true, "Should have thrown!");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

    }
}
