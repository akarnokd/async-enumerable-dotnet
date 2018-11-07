// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class TakeUntilTest
    {
        [Fact]
        public async void Normal()
        {
            var t = 500;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 3000;
            }
            var disposedMain = 0;
            var disposedOther = 0;

            await AsyncEnumerable.Range(1, 5)
                .DoOnDispose(() => disposedMain++)
                .TakeUntil(
                    AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(t))
                    .DoOnDispose(() => disposedOther += 2)
                )
                .AssertResult(1, 2, 3, 4, 5);

            Assert.Equal(1, disposedMain);
            Assert.Equal(2, disposedOther);
        }

        [Fact]
        public async void Until()
        {
            var disposedMain = 0;
            var disposedOther = 0;

            await AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(500))
                .DoOnDispose(() => disposedMain++)
                .TakeUntil(
                    AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(100))
                    .DoOnDispose(() => disposedOther += 2)
                )
                .AssertResult();

            Assert.Equal(1, disposedMain);
            Assert.Equal(2, disposedOther);
        }

        [Fact]
        public async void MainError()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .TakeUntil(AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200)))
                .AssertFailure(typeof(InvalidOperationException));

        }

        
        [Fact]
        public async void OtherError()
        {
            await AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(200))
                .TakeUntil(AsyncEnumerable.Error<int>(new InvalidOperationException()))
                .AssertFailure(typeof(InvalidOperationException));

        }

    }
}
