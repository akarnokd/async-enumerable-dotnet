// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ToObservable<T> : IObservable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        public ToObservable(IAsyncEnumerable<T> source)
        {
            _source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var cts = new CancellationTokenSource();
            var en = _source.GetAsyncEnumerator(cts.Token);
            var handler = new ToObservableHandler(observer, en, cts);
            handler.MoveNext();
            return handler;
        }

        private sealed class ToObservableHandler : IDisposable
        {
            private readonly IObserver<T> _downstream;

            private readonly IAsyncEnumerator<T> _source;

            private readonly CancellationTokenSource _cts;

            private int _wip;

            private int _dispose;

            public ToObservableHandler(IObserver<T> downstream, IAsyncEnumerator<T> source, CancellationTokenSource cts)
            {
                _downstream = downstream;
                _source = source;
                _cts = cts;
            }

            internal void MoveNext()
            {
                QueueDrainHelper.MoveNext(_source, ref _wip, ref _dispose, MainHandlerAction, this);
            }

            private static readonly Action<Task<bool>, object> MainHandlerAction =
                (t, state) => ((ToObservableHandler) state).HandleMain(t);

            private bool TryDispose()
            {
                if (Interlocked.Decrement(ref _dispose) != 0)
                {
                    _source.DisposeAsync();
                    return false;
                }

                return true;
            }
            
            private void HandleMain(Task<bool> task)
            {
                if (task.IsCanceled)
                {
                    if (TryDispose())
                    {
                        _downstream.OnError(new OperationCanceledException());
                    }
                }
                else if (task.IsFaulted)
                {
                    if (TryDispose())
                    {
                        _downstream.OnError(ExceptionHelper.Extract(task.Exception));
                    }
                }
                else
                {
                    if (task.Result)
                    {
                        var v = _source.Current;
                        if (TryDispose())
                        {
                            _downstream.OnNext(v);
                            MoveNext();
                        }
                    }
                    else
                    {
                        if (TryDispose())
                        {
                            _downstream.OnCompleted();
                        }
                    }
                }

            }

            public void Dispose()
            {
                _cts.Cancel();
                if (Interlocked.Increment(ref _dispose) == 1)
                {
                    _source.DisposeAsync();
                }
            }
        }
    }
}
