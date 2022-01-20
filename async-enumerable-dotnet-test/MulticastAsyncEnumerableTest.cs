// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class MulticastAsyncEnumerableTest
    {
        [Fact]
        public async Task Normal_No_Consumers()
        {
            var push = new MulticastAsyncEnumerable<int>();

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await push.AssertResult();
        }

        [Fact]
        public async Task Error_No_Consumers()
        {
            var push = new MulticastAsyncEnumerable<int>();

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await push.AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async Task Normal_One_Consumer()
        {
            var push = new MulticastAsyncEnumerable<int>();

            var en1 = push.GetAsyncEnumerator(default);

            var task = Task.Run(async () =>
            {
                await en1.AssertResult(1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task;
        }

        [Fact]
        public async Task Error_One_Consumer()
        {
            var push = new MulticastAsyncEnumerable<int>();

            var en1 = push.GetAsyncEnumerator(default);

            var task = Task.Run(async () =>
            {
                await en1.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task;
        }

        [Fact]
        public async Task Normal_2_Consumers()
        {
            var push = new MulticastAsyncEnumerable<int>();

            var en1 = push.GetAsyncEnumerator(default);

            var task1 = Task.Run(async () =>
            {
                await en1.AssertResult(1, 2, 3, 4, 5);
            });

            var en2 = push.GetAsyncEnumerator(default);
            var task2 = Task.Run(async () =>
            {
                await en2.AssertResult(1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task1;
            await task2;
        }

        [Fact]
        public async Task Error_2_Consumers()
        {
            var push = new MulticastAsyncEnumerable<int>();

            var en1 = push.GetAsyncEnumerator(default);


            var en2 = push.GetAsyncEnumerator(default);

            var task1 = Task.Run(async () =>
            {
                await en1.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            });
            var task2 = Task.Run(async () =>
            {
                await en2.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task1;
            await task2;
        }

        [Fact]
        public async Task Normal_2_Consumers_One_Take()
        {
            var push = new MulticastAsyncEnumerable<int>();

            var en1 = push.Take(3).GetAsyncEnumerator(default);

            var task1 = Task.Run(async () =>
            {
                await en1.AssertResult(1, 2, 3);
            });

            var en2 = push.GetAsyncEnumerator(default);
            var task2 = Task.Run(async () =>
            {
                await en2.AssertResult(1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task1;
            await task2;
        }

        [Fact]
        public async Task HasConsumers()
        {
            var push = new MulticastAsyncEnumerable<int>();

            Assert.False(push.HasConsumers);

            var en = push.GetAsyncEnumerator(default);

            Assert.True(push.HasConsumers);

            await en.DisposeAsync();

            Assert.False(push.HasConsumers);
        }

        [Fact]
        public async Task Complete()
        {
            var push = new MulticastAsyncEnumerable<int>();

            Assert.False(push.HasConsumers);

            var en = push.GetAsyncEnumerator(default);

            Assert.True(push.HasConsumers);

            await push.Complete();

            Assert.False(push.HasConsumers);

            await en.DisposeAsync();

            Assert.False(push.HasConsumers);
        }
    }
}
