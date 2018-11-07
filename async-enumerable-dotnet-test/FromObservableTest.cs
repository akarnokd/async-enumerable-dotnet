// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

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
        public async void Normal_From()
        {
            var result = AsyncEnumerable.FromObservable(new ObservableRange(1, 6));

            await result.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Long()
        {
            const int n = 1_000_000;
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

        [Fact]
        public async void Error()
        {
            await new ObservableError()
                .ToAsyncEnumerable()
                .AssertFailure(typeof(InvalidOperationException));

        }

        private sealed class ObservableError : IObservable<int>
        {
            public IDisposable Subscribe(IObserver<int> observer)
            {
                observer.OnError(new InvalidOperationException());
                return new EmptyDisposable();
            }

            private sealed class EmptyDisposable : IDisposable
            {
                public void Dispose()
                {
                    // deliberately no-op
                }
            }
        }
        
        private sealed class ObservableRange : IObservable<int>
        {
            private readonly int _start;
            private readonly int _end;

            public ObservableRange(int start, int end)
            {
                _start = start;
                _end = end;
            }

            public IDisposable Subscribe(IObserver<int> observer)
            {
                var cancel = new CancellationTokenSource();

                Task.Factory.StartNew(() =>
                {
                    for (var i = _start; i != _end && !cancel.IsCancellationRequested; i++)
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

            private sealed class Disposer : IDisposable
            {
                private readonly CancellationTokenSource _cts;

                public Disposer(CancellationTokenSource cts)
                {
                    _cts = cts;
                }

                public void Dispose()
                {
                    _cts.Cancel();
                }
            }
        }
    }
}
