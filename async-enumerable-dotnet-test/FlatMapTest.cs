using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class FlatMapTest
    {
        [Fact]
        public async void Simple()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 5))
                ;

            // FIXME plain range should keep ordering?
            await result.AssertResultSet(
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async void Simple_MaxConcurrency_1()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 5), 1)
                ;

            await result.AssertResult(
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async void Simple_MaxConcurrency_2()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 5), 2)
                ;

            await result.AssertResultSet(
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }


        [Fact]
        public async void Simple_MaxConcurrency_8()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 5), 8)
                ;

            await result.AssertResultSet(
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async void Simple_MaxConcurrency_1_Prefetch_1()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .Map(v =>
                {
                    Console.WriteLine(v);
                    return v;
                })
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 5)
                .Map(w =>
                {
                    Console.WriteLine(w);
                    return w;
                })
                , 1, 1)
                ;

            await result.AssertResultSet(
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async void OutOfOrder()
        {
            var result = AsyncEnumerable.FromArray(
                    100, 300, 200, 0, 500, 400
                )
                .FlatMap(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(v)).Map(w => v))
            ;

            await result.AssertResult(0, 100, 200, 300, 400, 500);
        }

        [Fact]
        public async void Just_Mapped_To_Range()
        {
            var result = AsyncEnumerable.Range(1, 1)
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 5))
                ;

            await result.AssertResult(
                    10, 11, 12, 13, 14
                );
        }

        [Fact]
        public async void Range_Mapped_To_Just()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 1))
                ;

            await result.AssertResultSet(
                    10, 20, 30, 40, 50
                );
        }

        [Fact]
        public async void Range_Mapped_To_Just_Max1()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 1), 1)
                ;

            await result.AssertResult(
                    10, 20, 30, 40, 50
                );
        }

        [Fact]
        public async void Take()
        {
            var disposed = 0;

            var result = AsyncEnumerable.Range(1, 1)
                .DoOnDispose(() => disposed++)
                .FlatMap(v => AsyncEnumerable.Range(v, 5)
                    .DoOnDispose(() => disposed++)
                )
                .Take(3)
                ;

            await result.AssertResult(1, 2, 3);

            Assert.Equal(2, disposed);
        }
    }
}