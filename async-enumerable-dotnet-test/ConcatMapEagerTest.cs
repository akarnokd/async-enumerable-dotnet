// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System;

namespace async_enumerable_dotnet_test
{
    public class ConcatMapEagerTest
    {
        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMapEager(v => AsyncEnumerable.Range(v * 10, 5))
                .AssertResult(
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async void Normal_Take()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMapEager(v => AsyncEnumerable.Range(v * 10, 5))
                .Take(7)
                .AssertResult(
                    10, 11, 12, 13, 14,
                    20, 21
                );
        }

        [Fact]
        public async void Normal_Params()
        {
            await AsyncEnumerable.ConcatEager(
                    AsyncEnumerable.Range(1, 5),
                    AsyncEnumerable.Range(6, 5)
                )
                .AssertResult(
                    1, 2, 3, 4, 5, 6, 7, 8, 9, 10
                );
        }

        [Fact]
        public async void Normal_Params_MaxConcurrency()
        {
            await AsyncEnumerable.ConcatEager(1,
                    AsyncEnumerable.Range(1, 5),
                    AsyncEnumerable.Range(6, 5)
                )
                .AssertResult(
                    1, 2, 3, 4, 5, 6, 7, 8, 9, 10
                );
        }

        [Fact]
        public async void Normal_Params_MaxConcurrency_Prefetch()
        {
            await AsyncEnumerable.ConcatEager(1, 1,
                    AsyncEnumerable.Range(1, 5),
                    AsyncEnumerable.Range(6, 5)
                )
                .AssertResult(
                    1, 2, 3, 4, 5, 6, 7, 8, 9, 10
                );
        }

        [Fact]
        public async void Nested_Normal()
        {
            await AsyncEnumerable.FromArray(
                    AsyncEnumerable.Range(1, 5),
                    AsyncEnumerable.Range(6, 5)
                )
                .ConcatEager()
                .AssertResult(
                    1, 2, 3, 4, 5, 6, 7, 8, 9, 10
                );
        }

        [Fact]
        public async void Main_Error()
        {
            await AsyncEnumerable.Range(1, 5).WithError(new InvalidOperationException())
                .ConcatMapEager(v => AsyncEnumerable.Range(v * 10, 5))
                .AssertFailure(typeof(InvalidOperationException),
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async void Inner_Error()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMapEager(v => {
                    var res = AsyncEnumerable.Range(v * 10, 5);
                    if (v == 3)
                    {
                        res = res.WithError(new InvalidOperationException());
                    }
                    return res;
                })
                .AssertFailure(typeof(InvalidOperationException),
                    10, 11, 12, 13, 14,
                    20, 21, 22, 23, 24,
                    30, 31, 32, 33, 34,
                    40, 41, 42, 43, 44,
                    50, 51, 52, 53, 54
                );
        }

        [Fact]
        public async void MaxConcurrency_Prefetch_Matrix()
        {
            for (var concurrency = 1; concurrency < 7; concurrency++)
            {
                for (var prefetch = 1; prefetch < 7; prefetch++)
                {
                    await AsyncEnumerable.Range(1, 5)
                        .ConcatMapEager(v => AsyncEnumerable.Range(v * 10, 5), concurrency, prefetch)
                        .AssertResult(
                            10, 11, 12, 13, 14,
                            20, 21, 22, 23, 24,
                            30, 31, 32, 33, 34,
                            40, 41, 42, 43, 44,
                            50, 51, 52, 53, 54
                        );
                }
            }
        }

        [Fact]
        public async void Mapper_Crash()
        {
            await AsyncEnumerable.Range(1, 5)
                .ConcatMapEager<int, int>(v => throw new InvalidOperationException())
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Take()
        {
            await TestHelper.TimeSequence(0, 200, 400, 600)
                .ConcatMapEager(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(100)))
                .Take(1)
                .AssertResult(0L);
        }
    }
}
