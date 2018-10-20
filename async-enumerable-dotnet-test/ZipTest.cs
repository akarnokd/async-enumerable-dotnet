using System;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class ZipTest
    {
        [Fact]
        public async void SameLength()
        {
            var source = AsyncEnumerable.FromArray(1, 2, 3, 4);

            var result = AsyncEnumerable.Zip(v =>
            {
                var sum = 0;
                foreach (var a in v)
                {
                    sum += a;
                }
                return sum;
            }, source, source, source);

            await TestHelper.AssertResult(result, 3, 6, 9, 12);
        }

        [Fact]
        public async void DifferentLengths()
        {
            var source1 = AsyncEnumerable.FromArray(1, 2, 3, 4);
            var source2 = AsyncEnumerable.FromArray(1, 2, 3);
            var source3 = AsyncEnumerable.FromArray(1, 2, 3, 4, 5);

            var result = AsyncEnumerable.Zip(v =>
            {
                var sum = 0;
                foreach (var a in v)
                {
                    sum += a;
                }
                return sum;
            }, source1, source2, source3);

            await TestHelper.AssertResult(result, 3, 6, 9);
        }
    }
}
