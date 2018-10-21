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

            result.Subscribe(consumer);

            await consumer.TerminateTask;

            Assert.Equal(new List<int>(new[] { 1, 2, 3, 4, 5 }), consumer.values);
            Assert.Null(consumer.error);
            Assert.True(consumer.completed);
        }

        internal sealed class BasicObserver<T> : IObserver<T>
        {
            internal readonly IList<T> values = new List<T>();
            internal bool completed;
            internal Exception error;

            readonly TaskCompletionSource<bool> terminate = new TaskCompletionSource<bool>();

            public void OnCompleted()
            {
                completed = true;
                terminate.TrySetResult(true);
            }

            public void OnError(Exception error)
            {
                this.error = error;
                terminate.TrySetResult(true);
            }

            public void OnNext(T value)
            {
                values.Add(value);
            }

            public Task TerminateTask => terminate.Task;
        }
    }
}
