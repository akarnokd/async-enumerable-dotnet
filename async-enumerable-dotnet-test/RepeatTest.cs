// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class RepeatTest
    {
        [Fact]
        public async void Unlimited()
        {
            await AsyncEnumerable.Just(1)
                .Repeat()
                .Take(5)
                .AssertResult(1, 1, 1, 1, 1);
        }

        [Fact]
        public async void Limited()
        {
            await AsyncEnumerable.Range(1, 2)
                .Repeat(3)
                .AssertResult(1, 2, 1, 2, 1, 2);
        }

        [Fact]
        public async void Limited_Condition()
        {
            await AsyncEnumerable.Range(1, 2)
                .Repeat(n => n < 2)
                .AssertResult(1, 2, 1, 2, 1, 2);
        }

        [Fact]
        public async void Limited_Condition_Task()
        {
            await AsyncEnumerable.Range(1, 2)
                .Repeat(async n => {
                    await Task.Delay(100);
                    return n < 2;
                })
                .AssertResult(1, 2, 1, 2, 1, 2);
        }
    }
}
