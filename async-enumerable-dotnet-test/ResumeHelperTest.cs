// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using System.Threading.Tasks;
using async_enumerable_dotnet.impl;

namespace async_enumerable_dotnet_test
{
    public class ResumeHelperTest
    {
        private TaskCompletionSource<bool> _tcs;
        
        [Fact]
        public void Value_Cancelled()
        {
            var source = new TaskCompletionSource<bool>();
            source.TrySetCanceled();
            
            ResumeHelper.Complete(ref _tcs, new ValueTask(source.Task));
            
            Assert.True(_tcs.Task.IsCanceled);
        }

        [Fact]
        public void Value_Error()
        {
            var source = new TaskCompletionSource<bool>();
            var ex = new InvalidOperationException();
            source.TrySetException(ex);

            ResumeHelper.Complete(ref _tcs, new ValueTask(source.Task));

            Assert.True(_tcs.Task.IsFaulted);
            Assert.Same(ex, ExceptionHelper.Extract(ExceptionHelper.Extract(_tcs.Task.Exception)));
        }

        
        [Fact]
        public void Value_Success()
        {
            var source = new TaskCompletionSource<bool>();
            source.TrySetResult(true);

            ResumeHelper.Complete(ref _tcs, new ValueTask(source.Task));

            Assert.True(_tcs.Task.IsCompleted);
            Assert.True(_tcs.Task.Result);
        }

        [Fact]
        public async Task Async_Cancelled()
        {
            var source = new TaskCompletionSource<bool>();

            ResumeHelper.Complete(ref _tcs, new ValueTask(source.Task));

            source.TrySetCanceled();

            try
            {
                await _tcs.Task;
            }
            catch (Exception)
            {
                // ignored
            }

            Assert.True(_tcs.Task.IsCanceled);
        }

        [Fact]
        public async Task Async_Error()
        {
            var source = new TaskCompletionSource<bool>();

            ResumeHelper.Complete(ref _tcs, new ValueTask(source.Task));

            var ex = new InvalidOperationException();
            source.TrySetException(ex);

            try
            {
                await _tcs.Task;
            }
            catch (AggregateException g)
            {
                Assert.Same(ex, ExceptionHelper.Extract(ExceptionHelper.Extract(g)));
            }
            
            Assert.True(_tcs.Task.IsFaulted);
        }

        
        [Fact]
        public async Task Async_Success()
        {
            var source = new TaskCompletionSource<bool>();

            ResumeHelper.Complete(ref _tcs, new ValueTask(source.Task));

            source.TrySetResult(true);

            await _tcs.Task;
            
            Assert.True(_tcs.Task.IsCompleted);
            Assert.True(_tcs.Task.Result);
        }

    }
}
