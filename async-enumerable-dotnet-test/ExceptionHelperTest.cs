using System;
using Xunit;
using async_enumerable_dotnet.impl;

namespace async_enumerable_dotnet_test
{
    public class ExceptionHelperTest
    {
        private Exception _error;
        
        [Fact]
        public void AddException()
        {
            var ex = new InvalidOperationException();
            Assert.True(ExceptionHelper.AddException(ref _error, ex));
            
            Assert.Same(ex, _error);
        }
        
        [Fact]
        public void AddException_Existing()
        {
            _error = new IndexOutOfRangeException();
            var ex = new InvalidOperationException();
            Assert.True(ExceptionHelper.AddException(ref _error, ex));
            
            Assert.True(_error is AggregateException);
            
            var g = (AggregateException)_error;
            
            Assert.Equal(2, g.InnerExceptions.Count);
            
            Assert.True(g.InnerExceptions[0] is IndexOutOfRangeException);
            Assert.True(g.InnerExceptions[1] is InvalidOperationException);
        }

        [Fact]
        public void AddException_Terminated()
        {
            var ex = new InvalidOperationException();
            
            Assert.True(ExceptionHelper.AddException(ref _error, ex));
            
            Assert.Same(ExceptionHelper.Terminate(ref _error), ex);
        }

        [Fact]
        public void AddException_Terminated_2()
        {
            Assert.Same(ExceptionHelper.Terminate(ref _error), null);

            var ex = new InvalidOperationException();

            Assert.False(ExceptionHelper.AddException(ref _error, ex));
        }

        [Fact]
        public void Terminate()
        {
            Assert.Same(ExceptionHelper.Terminate(ref _error), null);
            
            Assert.Same(ExceptionHelper.Terminate(ref _error), ExceptionHelper.Terminated);
        }

        [Fact]
        public void Unaggregate_Aggregated_Solo()
        {
            var ex = new InvalidOperationException();

            Assert.Same(ExceptionHelper.Unaggregate(new AggregateException(ex)), ex);
        }

        [Fact]
        public void Unaggregate_Aggregated_Not_Solo()
        {
            var ex = new InvalidOperationException();
            var ex2 = new IndexOutOfRangeException();

            var g = new AggregateException(ex, ex2);
            
            Assert.Same(ExceptionHelper.Unaggregate(g), g);
        }

        [Fact]
        public void Unaggregate_Not_Aggregated()
        {
            var ex = new InvalidOperationException();

            Assert.Same(ExceptionHelper.Unaggregate(ex), ex);
        }

    }
}
