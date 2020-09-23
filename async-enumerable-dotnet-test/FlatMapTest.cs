// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

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
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 5)
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
            var t = 100;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 1000;
            }

            var result = AsyncEnumerable.FromArray(
                    t, 3 * t, 2 * t, 0, 5 * t, 4 * t
                )
                .FlatMap(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(v)).Map(w => v))
            ;

            await result.AssertResult(0, t, 2 * t, 3 * t, 4 * t, 5 * t);
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
                .DoOnDispose(async () => {
                    await Task.Delay(100);
                    disposed++;
                })
                .FlatMap(v => AsyncEnumerable.Range(v, 5)
                    .DoOnDispose(() => disposed++)
                )
                .Take(3)
                ;

            await result.AssertResult(1, 2, 3);

            Assert.Equal(2, disposed);
        }

        [Fact]
        public async void Timer()
        {
            await AsyncEnumerable.Range(1, 10)
                .FlatMap(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(100)))
                .Take(5)
                .AssertResult(0L, 0L, 0L, 0L, 0L);
        }

        [Fact]
        public async void Mapper_Crash()
        {
            await AsyncEnumerable.Range(1, 5)
                .FlatMap<int, int>(v => throw new InvalidOperationException())
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void NoCancelDelay()
        {
            var start = DateTime.Now;
            try
            {
                await foreach (var item in async_enumerable_dotnet.AsyncEnumerable.Interval(TimeSpan.Zero, TimeSpan.FromSeconds(10))
                .Map(x => async_enumerable_dotnet.AsyncEnumerable.Just(x))
                .Merge())
                {
                    Console.WriteLine(item);

                    throw new Exception("expected");
                }

                throw new Exception("unexpected");
            }
            catch (Exception e) when (e.Message == "expected")
            {
                // expected
            }
            var end = DateTime.Now;

            if (end - start > TimeSpan.FromSeconds(5))
            {
                Assert.True(false, "Test took too much time");
            }
        }
    }
}
