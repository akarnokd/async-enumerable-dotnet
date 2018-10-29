// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet_test
{
    public class DoOnNextTest
    {
        [Fact]
        public async void Sync_Normal()
        {
            var list = new List<int>();
            await AsyncEnumerable.Range(1, 5)
                .DoOnNext(v => list.Add(v))
                .ForEach();

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list);
        }

        [Fact]
        public async void Async_Normal()
        {
            var list = new List<int>();
            await AsyncEnumerable.Range(1, 5)
                .DoOnNext(async v => {
                    await Task.Delay(100);
                    list.Add(v);
                })
                .ForEach();

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list);
        }
    }
}
