// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class ForEachTest
    {
        [Fact]
        public async Task Normal()
        {
            var sum = 0;
            await AsyncEnumerable.Range(1, 5)
                .ForEach(v => sum += v, onComplete: () => sum += 100);

            Assert.Equal(115, sum);
        }

        [Fact]
        public async Task Error()
        {
            var error = default(Exception);
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .ForEach(onError: e => error = e);
            
            Assert.NotNull(error);
            Assert.True(error is InvalidOperationException);
        }
    }
}
