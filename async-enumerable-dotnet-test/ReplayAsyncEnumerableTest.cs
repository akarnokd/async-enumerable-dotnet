// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

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

            for (var i = 1; i <= 5; i++)
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

            for (var i = 1; i <= 5; i++)
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

            for (var i = 1; i <= 5; i++)
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

            for (var i = 1; i <= 5; i++)
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

            for (var i = 1; i <= 5; i++)
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

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task1;
            await task2;
        }

        [Fact]
        public async void Unbounded_1_Normal()
        {
            var push = new ReplayAsyncEnumerable<int>();

            await push.Next(1);

            await push.Next(2);

            var t1 = push.AssertResult(1, 2, 3, 4, 5);

            await push.Next(3);

            await push.Next(4);

            await push.Next(5);

            await push.Complete();

            await t1;

            await push.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Sized_No_Consumers()
        {
            var push = new ReplayAsyncEnumerable<int>(10);

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await push.AssertResult(1, 2, 3, 4, 5);

            await push.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Sized_No_Consumers_Error()
        {
            var push = new ReplayAsyncEnumerable<int>(10);

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await push.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            await push.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Sized_One_Consumer()
        {
            var push = new ReplayAsyncEnumerable<int>(10);

            var en1 = push.GetAsyncEnumerator();

            var task1 = Task.Run(async () =>
            {
                await en1.AssertResult(1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task1;
        }

        [Fact]
        public async void Sized_One_Consumer_Error()
        {
            var push = new ReplayAsyncEnumerable<int>(10);

            var en1 = push.GetAsyncEnumerator();

            var task1 = Task.Run(async () =>
            {
                await en1.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task1;
        }

        [Fact]
        public async void Sized_2_Consumers()
        {
            var push = new ReplayAsyncEnumerable<int>(10);

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

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task1;
            await task2;
        }

        [Fact]
        public async void Sized_2_Consumers_Error()
        {
            var push = new ReplayAsyncEnumerable<int>(10);

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

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task1;
            await task2;
        }

        [Fact]
        public async void Sized_Bounded_1_Normal()
        {
            var push = new ReplayAsyncEnumerable<int>(1);

            await push.Next(1);

            await push.Next(2);

            var t1 = push.AssertResult(2, 3, 4, 5);

            await push.Next(3);

            await push.Next(4);

            await push.Next(5);

            await push.Complete();

            await t1;

            await push.AssertResult(5);
        }

        [Fact]
        public async void Sized_Bounded_2_Normal()
        {
            var push = new ReplayAsyncEnumerable<int>(2);

            await push.Next(1);

            await push.Next(2);

            await push.Next(3);

            var t1 = push.AssertResult(2, 3, 4, 5);

            await push.Next(4);

            await push.Next(5);

            await push.Complete();

            await t1;

            await push.AssertResult(4, 5);
        }

        [Fact]
        public async void TimedSized_No_Consumers()
        {
            var push = new ReplayAsyncEnumerable<int>(10, TimeSpan.FromHours(1));

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await push.AssertResult(1, 2, 3, 4, 5);

            await push.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void TimedSized_No_Consumers_Error()
        {
            var push = new ReplayAsyncEnumerable<int>(10, TimeSpan.FromHours(1));

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await push.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            await push.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Fact]
        public async void TimedSized_One_Consumer()
        {
            var push = new ReplayAsyncEnumerable<int>(10, TimeSpan.FromHours(1));

            var en1 = push.GetAsyncEnumerator();

            var task1 = Task.Run(async () =>
            {
                await en1.AssertResult(1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task1;
        }

        [Fact]
        public async void TimedSized_One_Consumer_Error()
        {
            var push = new ReplayAsyncEnumerable<int>(10, TimeSpan.FromHours(1));

            var en1 = push.GetAsyncEnumerator();

            var task1 = Task.Run(async () =>
            {
                await en1.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            });

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task1;
        }

        [Fact]
        public async void TimedSized_2_Consumers()
        {
            var push = new ReplayAsyncEnumerable<int>(10, TimeSpan.FromHours(1));

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

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Complete();

            await task1;
            await task2;
        }

        [Fact]
        public async void TimedSized_2_Consumers_Error()
        {
            var push = new ReplayAsyncEnumerable<int>(10, TimeSpan.FromHours(1));

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

            for (var i = 1; i <= 5; i++)
            {
                await push.Next(i);
            }
            await push.Error(new InvalidOperationException());

            await task1;
            await task2;
        }

        [Fact]
        public async void TimedSized_Bounded_1_Normal()
        {
            var push = new ReplayAsyncEnumerable<int>(1, TimeSpan.FromHours(1));

            await push.Next(1);

            await push.Next(2);

            var t1 = push.AssertResult(2, 3, 4, 5);

            await push.Next(3);

            await push.Next(4);

            await push.Next(5);

            await push.Complete();

            await t1;

            await push.AssertResult(5);
        }

        [Fact]
        public async void Timed_Bounded_1_Normal()
        {
            var timeNow = new [] { 0L };

            var push = new ReplayAsyncEnumerable<int>(10, TimeSpan.FromMilliseconds(100), () => timeNow[0]);

            await push.Next(1);

            timeNow[0] += 100;

            await push.Next(2);

            var t1 = push.AssertResult(2, 3, 4, 5);

            timeNow[0] += 100;

            await push.Next(3);

            timeNow[0] += 100;

            await push.Next(4);

            timeNow[0] += 100;

            await push.Next(5);

            await push.Complete();

            await t1;

            await push.AssertResult(5);

            timeNow[0] += 100;

            await push.AssertResult();
        }

        [Fact]
        public async void Unbounded_Long()
        {
            const long n = 1_000_000;
            var push = new ReplayAsyncEnumerable<long>();
            var t = push
                .Reduce((a, b) => a + b)
                .AssertResult(n * (n + 1) / 2);

            for (var i = 1; i <= n; i++)
            {
                await push.Next(i);
            }

            await push.Complete();

            await t;
        }

        [Fact]
        public async void Unbounded_Long_Halfway()
        {
            const long n = 1_000_000;
            var push = new ReplayAsyncEnumerable<long>();
            var t = default(ValueTask);

            for (var i = 1; i <= n; i++)
            {
                await push.Next(i);

                if (i * 2 == n)
                {
                    t = push
                    .Reduce((a, b) => a + b)
                    .AssertResult(n * (n + 1) / 2);
                }
            }

            await push.Complete();

            await t;
        }

        [Fact]
        public async void Sized_Long()
        {
            const long n = 1_000_000;
            var push = new ReplayAsyncEnumerable<long>(10);
            var t = push
                .Reduce((a, b) => a + b)
                .AssertResult(n * (n + 1) / 2);

            for (var i = 1; i <= n; i++)
            {
                await push.Next(i);
            }

            await push.Complete();

            await t;
        }

        [Fact]
        public async void Timed_Long()
        {
            const long n = 1_000_000;
            var push = new ReplayAsyncEnumerable<long>(TimeSpan.FromHours(1));
            var t = push
                .Reduce((a, b) => a + b)
                .AssertResult(n * (n + 1) / 2);

            for (var i = 1; i <= n; i++)
            {
                await push.Next(i);
            }

            await push.Complete();

            await t;
        }

        [Fact]
        public async void SizedTimed_Long()
        {
            const long n = 1_000_000;
            var push = new ReplayAsyncEnumerable<long>(10, TimeSpan.FromHours(1));
            var t = push
                .Reduce((a, b) => a + b)
                .AssertResult(n * (n + 1) / 2);

            for (var i = 1; i <= n; i++)
            {
                await push.Next(i);
            }

            await push.Complete();

            await t;
        }

        [Fact]
        public async void Sized_Long_Halfway()
        {
            const long n = 1_000_000;
            var push = new ReplayAsyncEnumerable<long>((int)n);
            var t = default(ValueTask);

            for (var i = 1; i <= n; i++)
            {
                await push.Next(i);

                if (i * 2 == n)
                {
                    t = push
                    .Reduce((a, b) => a + b)
                    .AssertResult(n * (n + 1) / 2);
                }
            }

            await push.Complete();

            await t;
        }

        [Fact]
        public async void TimedSized_Long_Halfway()
        {
            const long n = 1_000_000;
            var push = new ReplayAsyncEnumerable<long>((int)n, TimeSpan.FromHours(1));
            var t = default(ValueTask);

            for (var i = 1; i <= n; i++)
            {
                await push.Next(i);

                if (i * 2 == n)
                {
                    t = push
                    .Reduce((a, b) => a + b)
                    .AssertResult(n * (n + 1) / 2);
                }
            }

            await push.Complete();

            await t;
        }

        [Fact]
        public async void HasConsumers()
        {
            var push = new ReplayAsyncEnumerable<int>();

            Assert.False(push.HasConsumers);

            var en = push.GetAsyncEnumerator();

            Assert.True(push.HasConsumers);

            await en.DisposeAsync();

            Assert.False(push.HasConsumers);
        }

        [Fact]
        public async void Complete()
        {
            var push = new ReplayAsyncEnumerable<int>();

            Assert.False(push.HasConsumers);

            var en = push.GetAsyncEnumerator();

            Assert.True(push.HasConsumers);

            await push.Complete();

            Assert.False(push.HasConsumers);

            await en.DisposeAsync();

            Assert.False(push.HasConsumers);
        }
    }
}
