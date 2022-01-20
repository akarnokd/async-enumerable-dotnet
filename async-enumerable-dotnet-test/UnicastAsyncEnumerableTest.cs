// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class UnicastAsyncEnumerableTest
    {
        [Fact]
        public async Task Offline()
        {
            var push = new UnicastAsyncEnumerable<int>();

            Assert.False(push.HasConsumers);
            Assert.False(push.IsDisposed);
            
            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await push.AssertResult(1, 2, 3, 4, 5);

            Assert.False(push.HasConsumers);
            Assert.True(push.IsDisposed);
        }

        [Fact]
        public async Task Offline_Error()
        {
            var push = new UnicastAsyncEnumerable<int>();

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await push.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Fact]
        public async Task Online()
        {
            var push = new UnicastAsyncEnumerable<int>();

            Assert.False(push.HasConsumers);
            Assert.False(push.IsDisposed);

            var t = push.AssertResult(1, 2, 3, 4, 5);

            Assert.True(push.HasConsumers);
            Assert.False(push.IsDisposed);

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await t;
            
            Assert.False(push.HasConsumers);
            Assert.True(push.IsDisposed);

        }

        [Fact]
        public async Task Online_Error()
        {
            var push = new UnicastAsyncEnumerable<int>();

            var t = push.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await t;
        }

        [Fact]
        public async Task One_Consumer_Only()
        {
            var push = new UnicastAsyncEnumerable<int>();

            var t = push.AssertResult();

            await push.AssertFailure(typeof(InvalidOperationException));

            await push.AssertFailure(typeof(InvalidOperationException));

            await push.Complete();

            await t;
        }

        [Fact]
        public async Task Call_After_Done()
        {
            var push = new UnicastAsyncEnumerable<int>();
            await push.Complete();
            await push.Error(new InvalidOperationException());
            await push.Next(1);
            await push.Complete();

            await push.AssertResult();
        }


        [Fact]
        public async Task Call_After_Done_2()
        {
            var push = new UnicastAsyncEnumerable<int>();
            await push.Error(new InvalidOperationException());
            await push.Complete();
            await push.Next(1);
            await push.Error(new IndexOutOfRangeException());

            await push.AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async Task Next_After_Dispose()
        {
            var push = new UnicastAsyncEnumerable<int>();

            var t = push.Take(1).AssertResult(1);

            Assert.True(push.HasConsumers);
            Assert.False(push.IsDisposed);

            await push.Next(1);

            await t;

            Assert.False(push.HasConsumers);
            Assert.True(push.IsDisposed);

            await push.Next(2);
        }

        [Fact]
        public async Task Error_After_Dispose()
        {
            var push = new UnicastAsyncEnumerable<int>();

            var t = push.Take(1).AssertResult(1);

            await push.Next(1);

            await t;

            await push.Error(new InvalidOperationException());
        }

        [Fact]
        public async Task Complete_After_Dispose()
        {
            var push = new UnicastAsyncEnumerable<int>();

            var t = push.Take(1).AssertResult(1);

            await push.Next(1);

            await t;

            await push.Complete();
        }
    }
}
