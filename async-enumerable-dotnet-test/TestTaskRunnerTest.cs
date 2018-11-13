// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class TestTaskRunnerTest
    {
        [Fact]
        public void Simple()
        {
            var ttr = new TestTaskRunner();

            Assert.Equal(0L, ttr.Now);
            Assert.False(ttr.HasTasks);

            var t1 = ttr.CreateCompleteTask(100);

            Assert.True(ttr.HasTasks);
            Assert.False(t1.IsCompleted, "t1 is completed");

            ttr.AdvanceTimeBy(50);

            Assert.True(ttr.HasTasks);
            Assert.False(t1.IsCompleted, "t1 is completed");

            ttr.AdvanceTimeBy(50);

            Assert.False(ttr.HasTasks, "more tasks?");
            Assert.True(t1.IsCompleted, "t1 is not completed");
        }

        [Fact]
        public void Mixed()
        {
            var ttr = new TestTaskRunner();

            var ex = new InvalidOperationException();
            
            var t1 = ttr.CreateErrorTask(ex);
            var t2 = ttr.CreateErrorTask<int>(ex, 100);

            var t3 = ttr.CreateValueTask("hello", 100);
            var t4 = ttr.CreateValueTask("world", 200);

            var t5 = ttr.CreateCancelTask(200);
            
            var t6 = ttr.CreateCancelTask<int>(250);
            
            // T = 50 ------------------------------------------
            
            ttr.AdvanceTimeBy(50);
            Assert.Equal(50, ttr.Now);
            Assert.True(ttr.HasTasks);

            Assert.False(t2.IsCompleted);
            Assert.False(t3.IsCompleted);
            Assert.False(t4.IsCompleted);
            Assert.False(t5.IsCompleted);
            Assert.False(t6.IsCompleted);
            
            Assert.True(t1.IsFaulted);
            Assert.Equal(ex, t1.Exception.InnerException);

            // T = 100 ------------------------------------------

            ttr.AdvanceTimeBy(50);
            Assert.Equal(100, ttr.Now);
            Assert.True(ttr.HasTasks);

            Assert.False(t4.IsCompleted);
            Assert.False(t5.IsCompleted);
            Assert.False(t6.IsCompleted);

            Assert.True(t2.IsFaulted);
            Assert.Equal(ex, t2.Exception.InnerException);

            Assert.True(t3.IsCompleted);
            Assert.Equal("hello", t3.Result);

            // T = 200 ------------------------------------------
            
            ttr.AdvanceTimeBy(100);
            Assert.Equal(200, ttr.Now);
            Assert.True(ttr.HasTasks);

            Assert.False(t6.IsCompleted);

            Assert.True(t4.IsCompleted);
            Assert.Equal("world", t4.Result);

            Assert.True(t5.IsCanceled);

            // T = 250 ------------------------------------------
            
            ttr.AdvanceTimeBy(50);
            Assert.Equal(250, ttr.Now);
            Assert.False(ttr.HasTasks);

            Assert.True(t6.IsCanceled);
        }

        [Fact]
        public void Callback()
        {
            var ttr = new TestTaskRunner(50);
            var count = 0;

            var t1 = ttr.CreateActionTask(() => count++);

            var t2 = ttr.CreateActionTask(() => throw new InvalidOperationException(), 50);

            var t3 = ttr.CreateLambdaTask<long>(tcs => tcs.SetResult(ttr.Now), 100);
            var t4 = ttr.CreateLambdaTask<long>(tcs => throw new InvalidOperationException(), 100);
            
            Assert.Equal(0, count);
            
            // T = 50 ----------------------------------------------
            
            ttr.AdvanceTimeBy(0);

            Assert.True(t1.IsCompleted);
            Assert.Equal(1, count);

            // T = 100 ----------------------------------------------

            ttr.AdvanceTimeBy(50);
            
            Assert.True(t2.IsFaulted);
            Assert.True(t2.Exception.InnerException is InvalidOperationException);
            Assert.False(t3.IsCompleted);
            Assert.False(t4.IsCompleted);
            
            // T = 150 ----------------------------------------------
            
            ttr.AdvanceTimeBy(50);
            
            Assert.True(t3.IsCompleted);
            Assert.Equal(150L, t3.Result);

            Assert.True(t4.IsFaulted);
            Assert.True(t4.Exception.InnerException is InvalidOperationException);
        }
    }
}
