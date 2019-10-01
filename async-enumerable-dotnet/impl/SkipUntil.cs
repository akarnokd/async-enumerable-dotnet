// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SkipUntil<TSource, TOther> : IAsyncEnumerable<TSource>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly IAsyncEnumerable<TOther> _other;

        public SkipUntil(IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> other)
        {
            _source = source;
            _other = other;
        }

        public IAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var cancelMain = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var cancelOther = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var en = new SkipUntilEnumerator(_source.GetAsyncEnumerator(cancelMain.Token), _other.GetAsyncEnumerator(cancelOther.Token), cancelMain, cancelOther);
            en.MoveNextOther();
            en.MoveNextMain();
            return en;
        }

        private sealed class SkipUntilEnumerator : IAsyncEnumerator<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly IAsyncEnumerator<TOther> _other;

            private readonly CancellationTokenSource _cancelMain;

            private readonly CancellationTokenSource _cancelOther;

            private int _disposeMain;

            private int _disposeOther;

            private int _disposed;

            private Exception _disposeErrors;

            private readonly TaskCompletionSource<bool> _disposeTask;

            private Exception _error;
            private bool _done;
            private bool _hasValue;

            private TaskCompletionSource<bool> _resume;

            private int _gate;

            private int _wipMain;

            public SkipUntilEnumerator(IAsyncEnumerator<TSource> source, IAsyncEnumerator<TOther> other,
                CancellationTokenSource cancelMain, CancellationTokenSource cancelOther)
            {
                _source = source;
                _other = other;
                _disposeTask = new TaskCompletionSource<bool>();
                _cancelMain = cancelMain;
                _cancelOther = cancelOther;
                Volatile.Write(ref _disposed, 2);
            }

            public TSource Current { get; private set; }

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref _disposeMain) == 1)
                {
                    Dispose(_source);
                }

                if (Interlocked.Increment(ref _disposeOther) == 1)
                {
                    Dispose(_other);
                }

                await _disposeTask.Task;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    var ex = Volatile.Read(ref _error);
                    if (ex != null && ex != ExceptionHelper.Terminated)
                    {
                        throw ex;
                    }

                    var d = Volatile.Read(ref _done);
                    var e = Volatile.Read(ref _hasValue);

                    if (d && !e)
                    {
                        return false;
                    }

                    if (e)
                    {
                        _hasValue = false;
                        var next = false;
                        if (Volatile.Read(ref _gate) != 0)
                        {
                            next = true;
                            Current = _source.Current;
                        }
                        MoveNextMain();
                        if (next)
                        {
                            return true;
                        }
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            internal void MoveNextMain()
            {
                QueueDrainHelper.MoveNext(_source, ref _wipMain, ref _disposeMain, HandleMainAction, this);
            }

            private static readonly Action<Task<bool>, object> HandleMainAction =
                (t, state) => ((SkipUntilEnumerator)state).HandleMain(t);


            private bool TryDispose()
            {
                if (Interlocked.Decrement(ref _disposeMain) != 0)
                {
                    Dispose(_source);
                    return false;
                }

                return true;
            }
            private void HandleMain(Task<bool> t)
            {
                if (t.IsCanceled)
                {
                    Interlocked.CompareExchange(ref _error, new OperationCanceledException(), null);
                    _cancelOther.Cancel();
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
                else if (t.IsFaulted)
                {
                    Interlocked.CompareExchange(ref _error, ExceptionHelper.Extract(t.Exception), null);
                    _cancelOther.Cancel();
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
                else
                {
                    if (t.Result)
                    {
                        Volatile.Write(ref _hasValue, true);
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref _error, ExceptionHelper.Terminated, null);
                        Volatile.Write(ref _done, true);
                        _cancelOther.Cancel();
                    }

                    if (TryDispose())
                    {
                        Signal();
                    }
                }
            }

            internal void MoveNextOther()
            {
                if (Interlocked.Increment(ref _disposeOther) == 1){
                    _other.MoveNextAsync().AsTask()
                        .ContinueWith(HandleOtherAction, this, TaskContinuationOptions.ExecuteSynchronously);
                }
            }
            
            private static readonly Action<Task<bool>, object> HandleOtherAction =
                (t, state) => ((SkipUntilEnumerator)state).HandleOther(t);

            private void HandleOther(Task t)
            {
                if (Interlocked.Decrement(ref _disposeOther) != 0)
                {
                    Dispose(_other);
                }
                else
                {
                    if (t.IsCanceled)
                    {
                        Interlocked.CompareExchange(ref _error, new OperationCanceledException(), null);
                        _cancelMain.Cancel();
                        Signal();
                    }
                    else if (t.IsFaulted)
                    {
                        Interlocked.CompareExchange(ref _error, ExceptionHelper.Extract(t.Exception), null);
                        _cancelMain.Cancel();
                        Signal();
                    }
                    else
                    {
                        Interlocked.Exchange(ref _gate, 1);
                        Signal();
                    }

                    if (Interlocked.Increment(ref _disposeOther) == 1)
                    {
                        Dispose(_other);
                    }
                }
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }

            private void Dispose(IAsyncDisposable d)
            {
                d.DisposeAsync()
                    .AsTask().ContinueWith(DisposeHandlerAction, this);
            }

            private static readonly Action<Task, object> DisposeHandlerAction =
                (t, state) => ((SkipUntilEnumerator)state).DisposeHandler(t);

            private void DisposeHandler(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _disposed, ref _disposeErrors, _disposeTask);
            }
        }
    }
}
