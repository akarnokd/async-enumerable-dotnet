// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class CombineLatestTest
    {
        [Fact]
        public async Task Empty()
        {
            await AsyncEnumerable.CombineLatest<int, int>(v => v.Sum())
                .AssertResult();
        }

        [Fact]
        public async Task Single()
        {
            await AsyncEnumerable.CombineLatest(v => v.Sum() + 1,
                AsyncEnumerable.Just(1))
                .AssertResult(2);
        }

        [Fact]
        public async Task One_Item_Each()
        {
            await AsyncEnumerable.CombineLatest(v => v.Sum(), AsyncEnumerable.Just(1), AsyncEnumerable.Just(2))
                .AssertResult(3);
        }

        [Fact]
        public async Task One_Is_Empty()
        {
            await AsyncEnumerable.CombineLatest(v => v.Sum(), AsyncEnumerable.Empty<int>(), AsyncEnumerable.Just(2))
                .AssertResult();
        }

        [Fact]
        public async Task Two_Is_Empty()
        {
            await AsyncEnumerable.CombineLatest(v => v.Sum(), AsyncEnumerable.Just(1), AsyncEnumerable.Empty<int>())
                .AssertResult();
        }

        [Fact]
        public async Task ZigZag()
        {
            var t = 200;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 2000;
            }
            await AsyncEnumerable.CombineLatest(v => v.Sum(), 
                AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(t)),
                AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(t + t / 2), TimeSpan.FromMilliseconds(t)).Map(v => v * 10)
                )
                .AssertResult(11, 12, 22, 23, 33, 34, 44, 45, 55);
        }

        [Fact]
        public async Task Second_Many()
        {
            var t = 200;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 2000;
            }
            await AsyncEnumerable.CombineLatest(v => v.Sum(),
                AsyncEnumerable.Just(10L),
                AsyncEnumerable.Interval(1, 5, TimeSpan.FromMilliseconds(t + t / 2), TimeSpan.FromMilliseconds(t))
                )
                .AssertResult(11, 12, 13, 14, 15);
        }

        [Fact]
        public async Task Error()
        {
            await AsyncEnumerable.CombineLatest(v => v.Sum(),
                    AsyncEnumerable.Just(1), 
                    AsyncEnumerable.Just(2).ConcatWith(
                        AsyncEnumerable.Error<int>(new InvalidOperationException())
                    )
                )
                .AssertFailure(typeof(InvalidOperationException), 3);
        }
    }
}
