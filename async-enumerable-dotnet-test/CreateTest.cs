// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class CreateTest
    {
        [Fact]
        public async void Empty()
        {
            var result = AsyncEnumerable.Create<int>(async e =>
            {
                await Task.CompletedTask;
            });

            await result.AssertResult();
        }

        [Fact]
        public async void Range()
        {
            var result = AsyncEnumerable.Create<int>(async e =>
            {
                for (var i = 0; i < 10 && !e.DisposeAsyncRequested; i++)
                {
                    await e.Next(i);
                }
            });

            await result.AssertResult(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

    }
}
