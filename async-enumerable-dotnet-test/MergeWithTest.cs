// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using System.Threading.Tasks;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class MergeWithTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .MergeWith(AsyncEnumerable.Range(6, 5))
                .AssertResultSet(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }
    }
}
