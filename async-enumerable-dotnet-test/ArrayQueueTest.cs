// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet.impl;

namespace async_enumerable_dotnet_test
{
    public class ArrayQueueTest
    {
        [Fact]
        public void Normal()
        {
            for (var k = 2; k <= 128; k *= 2)
            {
                for (var j = 1; j < k * 4; j++) {
                    var q = new ArrayQueue<int>(k);

                    for (var i = 0; i < j; i++)
                    {
                        q.Enqueue(i);
                        Assert.True(q.Dequeue(out var v));
                        Assert.Equal(i, v);
                    }

                    Assert.False(q.Dequeue(out _));
                }
            }
        }

        [Fact]
        public void Grow()
        {
            for (var k = 2; k <= 128; k *= 2)
            {
                for (var j = 1; j < k * 4; j++)
                {
                    var q = new ArrayQueue<int>(k);

                    for (var i = 0; i < j; i++)
                    {
                        q.Enqueue(i);
                    }

                    for (var i = 0; i < j; i++)
                    {
                        Assert.True(q.Dequeue(out var v));
                        Assert.Equal(i, v);
                    }

                    Assert.False(q.Dequeue(out _));
                }
            }
        }
    }
}
