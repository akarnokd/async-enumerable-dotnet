using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Threading;

namespace async_enumerable_dotnet_test
{
    public class FromObservableTest
    {
        [Fact]
        public async void Normal()
        {
            var result = new ObservableRange(1, 6).ToAsyncEnumerable();

            await result.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Long()
        {
            var n = 1_000_000;
            var result = new ObservableRange(1, n).ToAsyncEnumerable();

            var expected = 1;
            var en = result.GetAsyncEnumerator();
            try
            {
                while (await en.MoveNextAsync())
                {
                    Assert.Equal(expected, en.Current);

                    expected++;
                }
            }
            finally
            {
                await en.DisposeAsync();
            }

            Assert.Equal(n, expected);
        }

        sealed class ObservableRange : IObservable<int>
        {
            readonly int start;
            readonly int end;

            public ObservableRange(int start, int end)
            {
                this.start = start;
                this.end = end;
            }

            public IDisposable Subscribe(IObserver<int> observer)
            {
                var cancel = new CancellationTokenSource();

                Task.Factory.StartNew(() =>
                {
                    for (var i = start; i != end && !cancel.IsCancellationRequested; i++)
                    {
                        observer.OnNext(i);
                    }

                    if (!cancel.IsCancellationRequested)
                    {
                        observer.OnCompleted();
                    }
                }, cancel.Token);

                return new Disposer(cancel);
            }

            sealed class Disposer : IDisposable
            {
                readonly CancellationTokenSource cts;

                public Disposer(CancellationTokenSource cts)
                {
                    this.cts = cts;
                }

                public void Dispose()
                {
                    cts.Cancel();
                }
            }
        }
    }
}
