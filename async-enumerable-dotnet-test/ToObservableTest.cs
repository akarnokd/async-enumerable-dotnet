// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet_test
{
    public class ToObservableTest
    {
        [Fact]
        public async void Normal()
        {
            var result = AsyncEnumerable.Range(1, 5)
                .ToObservable();

            var consumer = new BasicObserver<int>();

            using (result.Subscribe(consumer))
            {
                await consumer.TerminateTask;

                Assert.Equal(new List<int>(new[] {1, 2, 3, 4, 5}), consumer.Values);
                Assert.Null(consumer.Error);
                Assert.True(consumer.Completed);
            }
        }
        
        [Fact]
        public async void Error()
        {
            var ex = new InvalidOperationException();
            var result = AsyncEnumerable.Error<int>(ex)
                .ToObservable();

            var consumer = new BasicObserver<int>();

            using (result.Subscribe(consumer))
            {
                await consumer.TerminateTask;

                Assert.Empty(consumer.Values);
                Assert.Same(ex, consumer.Error);
                Assert.False(consumer.Completed);
            }
        }

        private sealed class BasicObserver<T> : IObserver<T>
        {
            internal readonly IList<T> Values = new List<T>();
            internal bool Completed;
            internal Exception Error;

            private readonly TaskCompletionSource<bool> _terminate = new TaskCompletionSource<bool>();

            public void OnCompleted()
            {
                Completed = true;
                _terminate.TrySetResult(true);
            }

            public void OnError(Exception error)
            {
                Error = error;
                _terminate.TrySetResult(true);
            }

            public void OnNext(T value)
            {
                Values.Add(value);
            }

            public Task TerminateTask => _terminate.Task;
        }
    }
}
