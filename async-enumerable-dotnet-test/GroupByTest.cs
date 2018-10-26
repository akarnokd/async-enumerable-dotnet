using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace async_enumerable_dotnet_test
{
    public class GroupByTest
    {
        [Fact]
        public async void Normal_Same_Group()
        {
            await AsyncEnumerable.Range(1, 10)
                .GroupBy(k => 1)
                .FlatMap(v => v.ToList())
                .AssertResult(
                    ListOf(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
                );
        }

        [Fact]
        public async void Normal_Distinct_Group()
        {
            await AsyncEnumerable.Range(1, 10)
                .GroupBy(k => k)
                .FlatMap(v => v.ToList())
                .AssertResultSet(
                    ListComparer<int>.Default,
                    ListOf(1),
                    ListOf(2),
                    ListOf(3),
                    ListOf(4),
                    ListOf(5),
                    ListOf(6),
                    ListOf(7),
                    ListOf(8),
                    ListOf(9),
                    ListOf(10)
                );
        }

        [Fact]
        public async void Normal_Mixed()
        {
            var disposed = 0;

            await AsyncEnumerable.Range(1, 10)
                .DoOnDispose(() => disposed++)
                .GroupBy(k => k % 2)
                .FlatMap(v => v.ToList())
                .AssertResultSet(
                    ListComparer<int>.Default,
                    ListOf(1, 3, 5, 7, 9),
                    ListOf(2, 4, 6, 8, 10)
                );

            Assert.Equal(1, disposed);
        }

        [Fact]
        public async void Normal_Ordered()
        {
            await AsyncEnumerable.Range(1, 10)
                .GroupBy(k => k < 6)
                .FlatMap(v => v.ToList())
                .AssertResultSet(
                    ListComparer<int>.Default,
                    ListOf(1, 2, 3, 4, 5),
                    ListOf(6, 7, 8, 9, 10)
                );
        }

        [Fact]
        public async void Take_2_Groups()
        {
            await AsyncEnumerable.Range(1, 10)
                .GroupBy(k => k % 3)
                .Take(2)
                .FlatMap(v => v.ToList())
                .AssertResultSet(
                    ListComparer<int>.Default,
                    ListOf(1, 4, 7, 10),
                    ListOf(2, 5, 8)
                );
        }

        [Fact]
        public async void Take_1_Of_Each_Group()
        {
            await AsyncEnumerable.Range(1, 10)
                .GroupBy(k => k)
                .FlatMap(v => v.Take(1))
                .AssertResultSet(
                    1, 2, 3, 4, 5, 6, 7, 8, 9, 10
                );
        }

        [Fact]
        public async void Take_1_Of_Each_Group_Two_Groups_Total()
        {
            var disposed = 0;

            await AsyncEnumerable.Range(1, 10)
                .DoOnDispose(() => disposed++)
                .GroupBy(k => k)
                .Take(2)
                .FlatMap(v => v.Take(1))
                .AssertResultSet(
                    1, 2
                );

            Assert.Equal(1, disposed);
        }

        static List<int> ListOf(params int[] values)
        {
            return new List<int>(values);
        }
    }
}