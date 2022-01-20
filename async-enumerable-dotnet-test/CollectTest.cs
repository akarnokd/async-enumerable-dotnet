// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class CollectTest
    {
        [Fact]
        public async Task Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .AssertResult(new List<int>(new[] { 1, 2, 3, 4, 5 }));
        }

        [Fact]
        public async Task Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .AssertResult(new List<int>());
        }

        [Fact]
        public async Task Initial_Crash()
        {
            await AsyncEnumerable.Empty<int>()
                .Collect<int, IList<int>>(() => throw new InvalidOperationException(), (a, b) => a.Add(b))
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
