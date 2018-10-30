// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System;

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
        public async ValueTask Range()
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

        [Fact]
        public async void Range_Loop()
        {
            for (int j = 0; j < 1000; j++)
            {
                await Range();
            }
        }

        [Fact]
        public async void Items_And_Error()
        {
            var result = AsyncEnumerable.Create<int>(async e =>
            {
                await e.Next(1);

                await e.Next(2);

                throw new InvalidOperationException();
            });

            await result.AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Fact]
        public async ValueTask Take()
        {
            await AsyncEnumerable.Create<int>(async e =>
            {
                for (var i = 0; i < 10 && !e.DisposeAsyncRequested; i++)
                {
                    await e.Next(i);
                }
            })
            .Take(5)
            .AssertResult(0, 1, 2, 3, 4);
        }

        [Fact]
        public async void Take_Loop()
        {
            for (int j = 0; j < 1000; j++)
            {
                await Take();
            }
        }
    }
}
