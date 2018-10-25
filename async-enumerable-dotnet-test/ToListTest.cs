using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet_test
{
    public class ToListTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .ToList()
                .AssertResult(new List<int>(new[] { 1, 2, 3, 4, 5 }));
        }

        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .ToList()
                .AssertResult(new List<int>());
        }

        [Fact]
        public async void Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .ToList()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
