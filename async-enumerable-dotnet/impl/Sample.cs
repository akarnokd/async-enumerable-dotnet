// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Sample<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly TimeSpan _period;

        private readonly bool _emitLast;

        public Sample(IAsyncEnumerable<T> source, TimeSpan period, bool emitLast)
        {
            _source = source;
            _period = period;
            _emitLast = emitLast;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new SampleEnumerator(_source.GetAsyncEnumerator(), _period, _emitLast);
            en.StartTimer();
            en.MoveNext();
            return en;
        }

        private sealed class SampleEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly TimeSpan _period;

            private readonly CancellationTokenSource _cts;

            private readonly bool _emitLast;

            private int _consumerWip;

            private TaskCompletionSource<bool> _resume;

            private object _timerLatest;

            private object _latest;
            private volatile bool _done;
            private Exception _error;

            private int _disposeWip;

            private TaskCompletionSource<bool> _disposeTask;

            public T Current { get; private set; }

            public SampleEnumerator(IAsyncEnumerator<T> source, TimeSpan period, bool emitLast)
            {
                _source = source;
                _period = period;
                _emitLast = emitLast;
                _disposeTask = new TaskCompletionSource<bool>();
                _cts = new CancellationTokenSource();
                Volatile.Write(ref _latest, EmptyHelper.EmptyIndicator);
            }

            public ValueTask DisposeAsync()
            {
                _cts.Cancel();
                if (Interlocked.Increment(ref _disposeWip) == 1)
                {
                    Interlocked.Exchange(ref _timerLatest, EmptyHelper.EmptyIndicator);
                    return _source.DisposeAsync();
                }
                return ResumeHelper.Await(ref _disposeTask);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = _done;
                    var v = Interlocked.Exchange(ref _latest, EmptyHelper.EmptyIndicator);

                    if (d && v == EmptyHelper.EmptyIndicator)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }

                    if (v != EmptyHelper.EmptyIndicator)
                    {
                        Current = (T)v;
                        return true;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            // FIXME timer drift
            internal void StartTimer()
            {
                Task.Delay(_period, _cts.Token)
                    .ContinueWith(HandleTimerAction, this, _cts.Token);
            }

            private static readonly Action<Task, object> HandleTimerAction =
                (t, state) => ((SampleEnumerator)state).HandleTimer(); 
            
            private void HandleTimer()
            {
                // take the saved timerLatest and make it available to MoveNextAsync
                // via latest
                Interlocked.Exchange(ref _latest, Interlocked.Exchange(ref _timerLatest, EmptyHelper.EmptyIndicator));

                Signal();
                StartTimer();
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref _consumerWip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref _disposeWip) == 1)
                        {
                            _source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(HandleAction, this);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref _consumerWip) != 0);
                }
            }
            
            private static readonly Action<Task<bool>, object> HandleAction =
                (t, state) => ((SampleEnumerator)state).Handler(t);

            private bool TryDispose()
            {
                if (Interlocked.Decrement(ref _disposeWip) != 0)
                {
                    Interlocked.Exchange(ref _timerLatest, EmptyHelper.EmptyIndicator);
                    ResumeHelper.Complete(ref _disposeTask, _source.DisposeAsync());
                    return false;
                }
                return true;
            }

            private void Handler(Task<bool> t)
            {
                if (t.IsFaulted)
                {
                    _cts.Cancel();
                    if (_emitLast)
                    {
                        Interlocked.Exchange(ref _latest, Interlocked.Exchange(ref _timerLatest, EmptyHelper.EmptyIndicator));
                    }
                    _error = ExceptionHelper.Extract(t.Exception);
                    _done = true;
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
                else if (t.Result)
                {
                    Interlocked.Exchange(ref _timerLatest, _source.Current);
                    if (TryDispose())
                    {
                        // the value will be picked up by the timer
                        MoveNext();
                    }
                }
                else
                {
                    _cts.Cancel();
                    if (_emitLast)
                    {
                        Interlocked.Exchange(ref _latest, Interlocked.Exchange(ref _timerLatest, EmptyHelper.EmptyIndicator));
                    }
                    _done = true;
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
            }
        }
    }
}
