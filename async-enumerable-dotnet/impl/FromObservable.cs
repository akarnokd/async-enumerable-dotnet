// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FromObservable<T> : IAsyncEnumerable<T>
    {
        private readonly IObservable<T> _source;

        public FromObservable(IObservable<T> source)
        {
            _source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var consumer = new FromObservableEnumerator();
            var d = _source.Subscribe(consumer);
            consumer.SetDisposable(d);
            return consumer;
        }

        private sealed class FromObservableEnumerator : IAsyncEnumerator<T>, IObserver<T>, IDisposable
        {
            private readonly ConcurrentQueue<T> _queue;

            private volatile bool _done;
            private Exception _error;

            private IDisposable _upstream;

            public T Current { get; private set; }

            private TaskCompletionSource<bool> _resume;

            public FromObservableEnumerator()
            {
                _queue = new ConcurrentQueue<T>();
            }

            public void Dispose()
            {
                // deliberately no-op as `this` is the terminal indicator
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                Interlocked.Exchange(ref _upstream, this)?.Dispose();
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = _done;
                    var success = _queue.TryDequeue(out var v);

                    if (d && !success)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }

                    if (success)
                    {
                        Current = v;
                        return true;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            public void OnCompleted()
            {
                _done = true;
                Signal();
            }

            public void OnError(Exception error)
            {
                _error = error;
                _done = true;
                Signal();
            }

            public void OnNext(T value)
            {
                _queue.Enqueue(value);
                Signal();
            }

            internal void SetDisposable(IDisposable d)
            {
                if (Interlocked.CompareExchange(ref _upstream, d, null) != null)
                {
                    d?.Dispose();
                } 
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }
        }
    }
}
