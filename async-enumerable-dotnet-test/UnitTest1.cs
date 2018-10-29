// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
        {
            await Task.CompletedTask;
        }
    }
}
