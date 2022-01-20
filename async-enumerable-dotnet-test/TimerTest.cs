// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class TimerTest
    {
        [Fact]
        public async Task Token()
        {
            var value = 0;
            
            var cts = new CancellationTokenSource();
#pragma warning disable 4014
            AsyncEnumerable.Timer(TimeSpan.FromSeconds(200), cts.Token)
                .DoOnNext(v => value = 1)
                .GetAsyncEnumerator(default)
                .MoveNextAsync();
#pragma warning restore 4014

            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(100);
            
            cts.Cancel();

            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(200);
            
            Assert.Equal(0, value);
        }

        [Fact]
        public async Task Normal()
        {
            var cts = new CancellationTokenSource();
            await AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(100), cts.Token)
                .AssertResult(0L);
        }
    }
}
