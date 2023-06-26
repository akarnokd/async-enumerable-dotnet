// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class SampleTest
    {
        [Fact]
        public async Task Normal()
        {
            var t = 200;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 2000;
            }

            await AsyncEnumerable.Range(1, 5)
                .FlatMap(v => 
                        AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(t * v - t / 2))
                        .Map(w => v)
                )
                .Sample(TimeSpan.FromMilliseconds(t * 2))
                .AssertResult(2, 4);
        }

        [Fact]
        public async Task Last()
        {
            await AsyncEnumerable.Range(1, 5)
                .Sample(TimeSpan.FromMilliseconds(500))
                .AssertResult();
        }

        [Fact]
        public async Task Normal_EmitLast()
        {
            var t = 200;
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                t = 2000;
            }

            await AsyncEnumerable.Range(1, 5)
                .FlatMap(v =>
                        AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(t * v - t / 2))
                        .Map(w => v)
                )
                .Sample(TimeSpan.FromMilliseconds(t * 2), true)
                .AssertResult(2, 4, 5);
        }

        [Fact]
        public async Task Last_EmitLast()
        {
            await AsyncEnumerable.Range(1, 5)
                .Sample(TimeSpan.FromMilliseconds(500), true)
                .AssertResult(5);
        }

        [Fact]
        public async Task Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .Sample(TimeSpan.FromMilliseconds(500))
                .AssertFailure(typeof(InvalidOperationException));
        }
        
        [Fact]
        public async Task Error_EmitLast()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .Sample(TimeSpan.FromMilliseconds(500), true)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async Task Take()
        {
            await TestHelper.TimeSequence(100, 300)
                .Sample(TimeSpan.FromMilliseconds(200))
                .Take(1)
                .AssertResult(100);
        }
    }
}
