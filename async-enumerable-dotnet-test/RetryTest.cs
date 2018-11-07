// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class RetryTest
    {
        [Fact]
        public async void Retry_Unlimited()
        {
            await AsyncEnumerable.Range(1, 2)
                .WithError(new Exception())
                .Retry()
                .Take(6)
                .AssertResult(1, 2, 1, 2, 1, 2);
        }

        [Fact]
        public async void Retry_Max()
        {
            await AsyncEnumerable.Range(1, 2)
                .WithError(new InvalidOperationException())
                .Retry(3)
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 1, 2, 1, 2);
        }

        [Fact]
        public async void Retry_Condition()
        {
            await AsyncEnumerable.Range(1, 2)
                .WithError(new InvalidOperationException())
                .Retry((idx, ex) => idx < 2)
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 1, 2, 1, 2);
        }

        [Fact]
        public async void Retry_Condition_False()
        {
            await AsyncEnumerable.Range(1, 2)
                .WithError(new InvalidOperationException())
                .Retry((idx, ex) => false)
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Fact]
        public async void Retry_Condition_Task()
        {
            await AsyncEnumerable.Range(1, 2)
                .WithError(new InvalidOperationException())
                .Retry(async (idx, ex) => {
                    await Task.Delay(100);
                    return idx < 2;
                })
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 1, 2, 1, 2);
        }
        
        [Fact]
        public async void Retry_Condition_Task_False()
        {
            await AsyncEnumerable.Range(1, 2)
                .WithError(new InvalidOperationException())
                .Retry(async (idx, ex) =>
                {
                    await Task.Delay(100);
                    return false;
                })
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }
    }
}
