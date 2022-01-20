// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class DoOnDisposeTest
    {
        [Fact]
        public async Task Sync_Normal()
        {
            var count = 0;
            await AsyncEnumerable.Range(1, 5)
                .DoOnDispose(() => count++)
                .AssertResult(1, 2, 3, 4, 5);
            
            Assert.Equal(1, count);
        }
        
        [Fact]
        public async Task Async_Normal()
        {
            var count = 0;
            await AsyncEnumerable.Range(1, 5)
                .DoOnDispose(async () =>
                {
                    await Task.Delay(100);
                    count++;
                })
                .AssertResult(1, 2, 3, 4, 5);
            
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Sync_Handler_Crash()
        {
            try
            {
                await AsyncEnumerable.Empty<int>()
                    .DoOnDispose(() => throw new InvalidOperationException())
                    .AssertResult();

                Assert.False(true, "Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Fact]
        public async Task Sync_Handler_Crash_Double_Crash()
        {
            try
            {
                await AsyncEnumerable.Empty<int>()
                    .DoOnDispose((Action)(() => throw new InvalidOperationException()))
                    .DoOnDispose((Action)(() => throw new InvalidOperationException()))
                    .AssertResult();

                Assert.False(true, "Should have thrown");
            }
            catch (AggregateException ex)
            {
                Assert.True(ex.InnerExceptions[0] is InvalidOperationException);
                Assert.True(ex.InnerExceptions[1] is InvalidOperationException);
            }
        }

        [Fact]
        public async Task Async_Handler_Crash()
        {
            try
            {
                await AsyncEnumerable.Empty<int>()
                    .DoOnDispose(async () =>
                    {
                        await Task.CompletedTask;
                        throw new InvalidOperationException();
                    })
                    .AssertResult();

                Assert.False(true, "Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Fact]
        public async Task Async_Handler_Crash_Double_Crash()
        {
            try
            {
                await AsyncEnumerable.Empty<int>()
                    .DoOnDispose(async () =>
                    {
                        await Task.CompletedTask;
                        throw new InvalidOperationException();
                    })
                    .DoOnDispose(async () =>
                    {
                        await Task.CompletedTask;
                        throw new InvalidOperationException();
                    })
                    .AssertResult();

                Assert.False(true, "Should have thrown");
            }
            catch (AggregateException ex)
            {
                Assert.True(ex.InnerExceptions[0] is InvalidOperationException);
                Assert.True(ex.InnerExceptions[1] is InvalidOperationException);
            }
        }

    }
}
