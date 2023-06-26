// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class FilterTest
    {
        [Fact]
        public async Task Sync_Normal()
        {
            var result = AsyncEnumerable.Range(1, 10)
                .Filter(v => v % 2 == 0);

            await result.AssertResult(2, 4, 6, 8, 10);
        }

        [Fact]
        public async Task Async_Normal()
        {
            var result = AsyncEnumerable.Range(1, 10)
                .Filter(async v => 
                {
                    await Task.Delay(100);
                    return v % 2 == 0;
                });

            await result.AssertResult(2, 4, 6, 8, 10);
        }
    }
}
