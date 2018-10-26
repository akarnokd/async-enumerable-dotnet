using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class BufferTest
    {
        [Fact]
        public async void Exact_Remainder()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(2)
                .AssertResult(
                    new[] { 1, 2 },
                    new[] { 3, 4 },
                    new[] { 5 }
                );
        }
        
        [Fact]
        public async void Exact_Remainder_SizeSkip()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(2, 2)
                .AssertResult(
                    new[] { 1, 2 },
                    new[] { 3, 4 },
                    new[] { 5 }
                );
        }

        [Fact]
        public async void Exact_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Buffer(2)
                .AssertResult();
        }

        [Fact]
        public async void Exact_Full()
        {
            await AsyncEnumerable.Range(1, 6)
                .Buffer(2)
                .AssertResult(
                    new[] { 1, 2 },
                    new[] { 3, 4 },
                    new[] { 5, 6 }
                );
        }

        [Fact]
        public async void Skip_2_Size_1()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(1, 2)
                .AssertResult(
                    new[] { 1 },
                    new[] { 3 },
                    new[] { 5 }
                );
        }

        [Fact]
        public async void Skip_2_Size_1_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Buffer(1, 2)
                .AssertResult();
        }

        [Fact]
        public async void Skip_3_Size_1_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Buffer(1, 3)
                .AssertResult();
        }

        [Fact]
        public async void Skip_3_Size_1()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(1, 3)
                .AssertResult(
                    new[] { 1 },
                    new[] { 4 }
                );
        }

        [Fact]
        public async void Skip_3_Size_2_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Buffer(2, 3)
                .AssertResult();
        }

        [Fact]
        public async void Skip_3_Size_2()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(2, 3)
                .AssertResult(
                    new[] { 1, 2 },
                    new[] { 4, 5 }
                );
        }

        [Fact]
        public async void Overlap_2_Skip_1()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(2, 1)
                .AssertResult(
                    new[] { 1, 2 },
                    new[] { 2, 3 },
                    new[] { 3, 4 },
                    new[] { 4, 5 },
                    new[] { 5 }
                );
        }

        [Fact]
        public async void Overlap_3_Skip_1()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(3, 1)
                .AssertResult(
                    new[] { 1, 2, 3 },
                    new[] { 2, 3, 4 },
                    new[] { 3, 4, 5 },
                    new[] { 4, 5 },
                    new[] { 5 }
                );
        }

        [Fact]
        public async void Overlap_3_Skip_2()
        {
            await AsyncEnumerable.Range(1, 5)
                .Buffer(3, 2)
                .AssertResult(
                    new[] { 1, 2, 3 },
                    new[] { 3, 4, 5 },
                    new[] { 5 }
                );
        }

        [Fact]
        public async void Overlap_2_Skip_1_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Buffer(2, 1)
                .AssertResult();
        }

        [Fact]
        public async void Overlap_3_Skip_1_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Buffer(3, 1)
                .AssertResult();
        }

        [Fact]
        public async void Overlap_3_Skip_2_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Buffer(3, 2)
                .AssertResult();
        }

    }
}
