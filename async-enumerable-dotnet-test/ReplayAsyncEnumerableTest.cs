using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_test
{
    public class ReplayAsyncEnumerableTest
    {
        [Fact]
        public async void Unbounded_No_Consumers()
        {
            var push = new ReplayAsyncEnumerable<int>();

            for (int i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await push.AssertResult(1, 2, 3, 4, 5);

            await push.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Unbounded_No_Consumers_Error()
        {
            var push = new ReplayAsyncEnumerable<int>();

            for (int i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await push.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            await push.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Unbounded_One_Consumer()
        {
            var push = new ReplayAsyncEnumerable<int>();

            var en1 = push.GetAsyncEnumerator();

            var task1 = Task.Run(async () =>
            {
                await en1.AssertResult(1, 2, 3, 4, 5);
            });

            for (int i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task1;
        }

        [Fact]
        public async void Unbounded_One_Consumer_Error()
        {
            var push = new ReplayAsyncEnumerable<int>();

            var en1 = push.GetAsyncEnumerator();

            var task1 = Task.Run(async () =>
            {
                await en1.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            });

            for (int i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task1;
        }

        [Fact]
        public async void Unbounded_2_Consumers()
        {
            var push = new ReplayAsyncEnumerable<int>();

            var en1 = push.GetAsyncEnumerator();
            var en2 = push.GetAsyncEnumerator();

            var task1 = Task.Run(async () =>
            {
                await en1.AssertResult(1, 2, 3, 4, 5);
            });
            var task2 = Task.Run(async () =>
            {
                await en2.AssertResult(1, 2, 3, 4, 5);
            });

            for (int i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task1;
            await task2;
        }

        [Fact]
        public async void Unbounded_2_Consumers_Error()
        {
            var push = new ReplayAsyncEnumerable<int>();

            var en1 = push.GetAsyncEnumerator();
            var en2 = push.GetAsyncEnumerator();

            var task1 = Task.Run(async () =>
            {
                await en1.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            });
            var task2 = Task.Run(async () =>
            {
                await en2.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            });

            for (int i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task1;
            await task2;
        }
    }
}
