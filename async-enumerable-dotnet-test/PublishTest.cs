// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using System.Threading.Tasks;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class PublishTest
    {
        [Fact]
        public async void Simple()
        {
            await AsyncEnumerable.Range(1, 5)
                .Publish(a => a.Map(v => v + 1))
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Fact]
        public async void Direct()
        {
            await AsyncEnumerable.Range(1, 5)
                .Publish(a => a)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Take()
        {
            await AsyncEnumerable.Range(1, 5)
                .Publish(a => a.Map(v => v + 1))
                .Take(3)
                .AssertResult(2, 3, 4);
        }

        [Fact]
        public async void Take_Inner()
        {
            await AsyncEnumerable.Range(1, 5)
                .Publish(a => a.Take(3))
                .AssertResult(1, 2, 3);
        }

        [Fact]
        public async void Take_Concat_Inner()
        {
            await AsyncEnumerable.Range(1, 5)
                .Publish(a => a.Take(3).ConcatWith(AsyncEnumerable.Range(4, 2)))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Multicast_Merge()
        {
            await AsyncEnumerable.Range(1, 5)
                .Publish(a => a.Take(3).MergeWith(a.Skip(3)))
                .AssertResultSet(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Multicast_Concat()
        {
            await AsyncEnumerable.Range(1, 5)
                .Publish(a => a.Take(3).ConcatWith(a))
                .AssertThen(list => {
                    // concat may not switch fast enough so publish just runs through
                    Assert.True(list.Count >= 3, "Items missing: " + list);
                });
        }


        [Fact]
        public async void Unrelated()
        {
            await AsyncEnumerable.Range(1, 5)
                .Publish(a => AsyncEnumerable.Range(1, 5)
                    .Take(3)
                    .ConcatWith(AsyncEnumerable.Range(4, 2))
                )
                .AssertResult(1, 2, 3, 4, 5);
        }
    }
}
