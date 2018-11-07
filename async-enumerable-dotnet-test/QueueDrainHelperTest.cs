// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Xunit;
using async_enumerable_dotnet.impl;

namespace async_enumerable_dotnet_test
{
    public class QueueDrainHelperTest
    {
        private int _disposeWip;
        private Exception _disposeError;
        private readonly TaskCompletionSource<bool> _disposeTask = new TaskCompletionSource<bool>();

        [Fact]
        public void DisposeCrash()
        {
            var task = Task.FromException(new InvalidOperationException());
            _disposeWip = 1;
            
            QueueDrainHelper.DisposeHandler(task, ref _disposeWip, ref _disposeError, _disposeTask);
            
            Assert.Equal(0, _disposeWip);
            Assert.Null(_disposeError);
            
            Assert.True(_disposeTask.Task.IsFaulted);
            
            Assert.True(_disposeTask.Task.Exception.InnerExceptions[0] is InvalidOperationException);
        }
    }
}
