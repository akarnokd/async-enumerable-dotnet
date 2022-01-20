// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Linq;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class FromEnumerableTest
    {
        [Fact]
        public async Task Normal()
        {
            var result = AsyncEnumerable.FromEnumerable(Enumerable.Range(1, 5))
                ;

            await result.AssertResult(1, 2, 3, 4, 5);
        }
    }
}
