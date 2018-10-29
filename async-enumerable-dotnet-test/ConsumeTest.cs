// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class ConsumeTest
    {
        [Fact]
        public async void Normal()
        {
            var push = new MulticastAsyncEnumerable<int>();

            var t1 = push.AssertResult(1, 2, 3, 4, 5);
            var t2 = push.AssertResult(1, 2, 3, 4, 5);

            await AsyncEnumerable.Range(1, 5)
                .Consume(push);

            await t1;
            await t2;
        }
    }
}
