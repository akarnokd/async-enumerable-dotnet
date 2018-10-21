using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet_test
{
    public class ToEnumerableTest
    {
        [Fact]
        public void Normal()
        {
            var list = new List<int>();
            foreach (var v in AsyncEnumerable.Range(1, 5).ToEnumerable())
            {
                list.Add(v);
            }

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list);
        }
    }
}
