// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class OnErrorResumeNextTest
    {
        [Fact]
        public async Task Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .OnErrorResumeNext(e => AsyncEnumerable.Range(6, 5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task Error_Switch()
        {
            await AsyncEnumerable.Error<int>(new Exception())
                .OnErrorResumeNext(e => AsyncEnumerable.Range(6, 5))
                .AssertResult(6, 7, 8, 9, 10);
        }

        [Fact]
        public async Task Handler_Crash()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(e => throw e)
                .AssertFailure(typeof(AggregateException));
        }

        [Fact]
        public async Task Error_DisposeSource()
        {
            var disposed = false;
            await AsyncEnumerable.Error<int>(new Exception())
                .DoOnDispose(() => disposed = true)
                .OnErrorResumeNext(e => AsyncEnumerable.Empty<int>())
                .AssertResult();
            Assert.True(disposed);
        }
    }
}
