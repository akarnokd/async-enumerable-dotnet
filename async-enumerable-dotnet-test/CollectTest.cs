using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet_test
{
    public class CollectTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .AssertResult(new List<int>(new[] { 1, 2, 3, 4, 5 }));
        }

        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .AssertResult(new List<int>());
        }
    }
}
