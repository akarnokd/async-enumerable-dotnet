// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading;
using Xunit;
using async_enumerable_dotnet.impl;

namespace async_enumerable_dotnet_test
{
    public class CancellationHelperTest
    {
        private CancellationTokenSource _cts;
        
        [Fact]
        public void Cancel()
        {
            Assert.True(CancellationHelper.Cancel(ref _cts));

            Assert.Same(_cts, CancellationHelper.Cancelled);

            Assert.False(CancellationHelper.Cancel(ref _cts));
        }
    }
}
