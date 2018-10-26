using System;
using System.Threading;
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
            
            ResumeHelper.ResumeWhen(new ValueTask(source.Task), ref _tcs);
            
            Assert.True(_tcs.Task.IsCanceled);
        }

        [Fact]
        public void Value_Error()
        {
            var source = new TaskCompletionSource<bool>();
            var ex = new InvalidOperationException();
            source.TrySetException(ex);
            
            ResumeHelper.ResumeWhen(new ValueTask(source.Task), ref _tcs);
            
            Assert.True(_tcs.Task.IsFaulted);
            Assert.Same(ex, ExceptionHelper.Unaggregate(ExceptionHelper.Unaggregate(_tcs.Task.Exception)));
        }

        
        [Fact]
        public void Value_Success()
        {
            var source = new TaskCompletionSource<bool>();
            source.TrySetResult(true);
            
            ResumeHelper.ResumeWhen(new ValueTask(source.Task), ref _tcs);
            
            Assert.True(_tcs.Task.IsCompleted);
            Assert.True(_tcs.Task.Result);
        }

        [Fact]
        public async void Async_Cancelled()
        {
            var source = new TaskCompletionSource<bool>();
            
            ResumeHelper.ResumeWhen(new ValueTask(source.Task), ref _tcs);

            source.TrySetCanceled();

            try
            {
                await _tcs.Task;
            }
            catch (Exception)
            {
                
            }

            Assert.True(_tcs.Task.IsCanceled);
        }

        [Fact]
        public async void Async_Error()
        {
            var source = new TaskCompletionSource<bool>();
            
            ResumeHelper.ResumeWhen(new ValueTask(source.Task), ref _tcs);

            var ex = new InvalidOperationException();
            source.TrySetException(ex);

            try
            {
                await _tcs.Task;
            }
            catch (AggregateException g)
            {
                Assert.Same(ex, ExceptionHelper.Unaggregate(ExceptionHelper.Unaggregate(g)));
            }
            
            Assert.True(_tcs.Task.IsFaulted);
        }

        
        [Fact]
        public async void Async_Success()
        {
            var source = new TaskCompletionSource<bool>();
            
            ResumeHelper.ResumeWhen(new ValueTask(source.Task), ref _tcs);
            
            source.TrySetResult(true);

            await _tcs.Task;
            
            Assert.True(_tcs.Task.IsCompleted);
            Assert.True(_tcs.Task.Result);
        }

    }
}
