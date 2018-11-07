// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class AnyTest
    {
        [Fact]
        public async void Sync_None()
        {
            await AsyncEnumerable.Range(1, 5)
                .Any(v => v > 6)
                .AssertResult(false);
        }
        
        [Fact]
        public async void Sync_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Any(v => v > 6)
                .AssertResult(false);
        }
        
        [Fact]
        public async void Sync_Match()
        {
            await AsyncEnumerable.Range(1, 5)
                .Any(v => v > 4)
                .AssertResult(true);
        }
        
        [Fact]
        public async void Sync_Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .Any(v => v > 6)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Sync_Crash()
        {
            await AsyncEnumerable.Range(1, 5)
                .Any((Func<int, bool>)(v => throw new InvalidOperationException()))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Async_None()
        {
            await AsyncEnumerable.Range(1, 5)
                .Any(async v =>
                {
                    await Task.Delay(100);
                    return v > 6;
                })
                .AssertResult(false);
        }

        [Fact]
        public async void Async_Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .Any(async v =>
                {
                    await Task.Delay(100);
                    return v > 6;
                })
                .AssertResult(false);
        }

        [Fact]
        public async void Async_Match()
        {
            await AsyncEnumerable.Range(1, 5)
                .Any(async v =>
                {
                    await Task.Delay(100);
                    return v > 4;
                })
                .AssertResult(true);
        }
        
        [Fact]
        public async void Async_Error()
        {
            await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .Any(async v =>
                {
                    await Task.Delay(100);
                    return v > 6;
                })
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Fact]
        public async void Async_Crash()
        {
            await AsyncEnumerable.Range(1, 5)
                .Any(async v =>
                {
                    await Task.Delay(100);
                    throw new InvalidOperationException();
                })
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
