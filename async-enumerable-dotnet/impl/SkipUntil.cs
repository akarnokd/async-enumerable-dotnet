// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

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

        public IAsyncEnumerator<TSource> GetAsyncEnumerator()
        {
            var en = new SkipUntilEnumerator(_source.GetAsyncEnumerator(), _other.GetAsyncEnumerator());
            en.MoveNextOther();
            en.MoveNextMain();
            return en;
        }

        private sealed class SkipUntilEnumerator : IAsyncEnumerator<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly IAsyncEnumerator<TOther> _other;

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

            public SkipUntilEnumerator(IAsyncEnumerator<TSource> source, IAsyncEnumerator<TOther> other)
            {
                _source = source;
                _other = other;
                _disposeTask = new TaskCompletionSource<bool>();
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
                    if (ex != null)
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
                if (Interlocked.Increment(ref _wipMain) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref _disposeMain) == 1)
                        {
                            _source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(HandleMainAction, this);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref _wipMain) != 0);
                }
            }

            private static readonly Action<Task<bool>, object> HandleMainAction =
                (t, state) => ((SkipUntilEnumerator)state).HandleMain(t);

            private void HandleMain(Task<bool> t)
            {
                if (Interlocked.Decrement(ref _disposeMain) != 0)
                {
                    Dispose(_source);
                }
                else if (t.IsFaulted)
                {
                    Interlocked.CompareExchange(ref _error, t.Exception, null);
                    Signal();
                }
                else
                {
                    if (t.Result)
                    {
                        Volatile.Write(ref _hasValue, true);
                    }
                    else
                    {
                        Volatile.Write(ref _done, true);
                    }
                    Signal();
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
                    if (t.IsFaulted)
                    {
                        Interlocked.CompareExchange(ref _error, t.Exception, null);
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
                if (t.IsFaulted)
                {
                    ExceptionHelper.AddException(ref _disposeErrors, t.Exception);
                }
                if (Interlocked.Decrement(ref _disposed) == 0)
                {
                    var ex = _disposeErrors;
                    if (ex != null)
                    {
                        _disposeTask.TrySetException(ex);
                    }
                    else
                    {
                        _disposeTask.TrySetResult(false);
                    }
                }
            }
        }
    }
}
