// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class WithLatestFrom<TSource, TOther, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly IAsyncEnumerable<TOther> _other;

        private readonly Func<TSource, TOther, TResult> _func;

        public WithLatestFrom(IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> other, Func<TSource, TOther, TResult> func)
        {
            _source = source;
            _other = other;
            _func = func;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            var en = new WithLatestFromEnumerator(_source.GetAsyncEnumerator(), _other.GetAsyncEnumerator(), _func);
            en.MoveNextOther();
            en.MoveNextMain();
            return en;
        }

        private sealed class WithLatestFromEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly IAsyncEnumerator<TOther> _other;

            private readonly Func<TSource, TOther, TResult> _func;

            public TResult Current { get; private set; }

            private TSource _sourceValue;
            private bool _sourceReady;
            private object _otherValue;

            private Exception _sourceError;
            private volatile bool _sourceDone;
            private Exception _otherError;
            private volatile bool _otherDone;

            private TaskCompletionSource<bool> _resume;

            private int _sourceDisposeWip;
            private int _sourceWip;

            private int _otherDisposeWip;
            private int _otherWip;

            private int _disposeWip;
            private Exception _disposeError;
            private readonly TaskCompletionSource<bool> _disposeTask;

            public WithLatestFromEnumerator(IAsyncEnumerator<TSource> source, IAsyncEnumerator<TOther> other, Func<TSource, TOther, TResult> func)
            {
                _source = source;
                _other = other;
                _func = func;
                _disposeWip = 2;
                _disposeTask = new TaskCompletionSource<bool>();
                Volatile.Write(ref _otherValue, EmptyHelper.EmptyIndicator);
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref _otherDisposeWip) == 1)
                {
                    Dispose(_other);
                }

                if (Interlocked.Increment(ref _sourceDisposeWip) == 1)
                {
                    Dispose(_source);
                }

                return new ValueTask(_disposeTask.Task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = _sourceDone;
                    var v = _sourceReady;
                    if (d)
                    {
                        var ex = _sourceError;
                        if (ex != null)
                        {
                            throw ex;
                        }
                        if (!v)
                        {
                            return false;
                        }
                    }
                    d = _otherDone;
                    var o = Volatile.Read(ref _otherValue);
                    if (d)
                    {
                        var ex = _otherError;
                        if (ex != null)
                        {
                            throw ex;
                        }
                        if (o == EmptyHelper.EmptyIndicator)
                        {
                            return false;
                        }
                    }

                    if (v)
                    {
                        _sourceReady = false;
                        if (o == EmptyHelper.EmptyIndicator)
                        {
                            MoveNextMain();
                            continue;
                        }

                        Current = _func(_sourceValue, (TOther)o);

                        MoveNextMain();
                        return true;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            internal void MoveNextMain()
            {
                QueueDrainHelper.MoveNext(_source, ref _sourceWip, ref _sourceDisposeWip, MainHandlerAction, this);
            }

            private static readonly Action<Task<bool>, object> MainHandlerAction = (t, state) => ((WithLatestFromEnumerator)state).MainHandler(t);

            private bool TryDisposeMain()
            {
                if (Interlocked.Decrement(ref _sourceDisposeWip) != 0)
                {
                    Dispose(_source);
                    return false;
                }
                return true;
            }
            
            private void MainHandler(Task<bool> t)
            {
                if (t.IsFaulted)
                {
                    _sourceError = ExceptionHelper.Extract(t.Exception);
                    _sourceDone = true;
                    if (TryDisposeMain())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
                else if (t.Result)
                {
                    _sourceValue = _source.Current;
                    _sourceReady = true;
                    if (TryDisposeMain())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
                else
                {
                    _sourceDone = true;
                    if (TryDisposeMain())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
            }

            internal void MoveNextOther()
            {
                QueueDrainHelper.MoveNext(_other, ref _otherWip, ref _otherDisposeWip, OtherHandlerAction, this);
            }

            private static readonly Action<Task<bool>, object> OtherHandlerAction = (t, state) => ((WithLatestFromEnumerator)state).OtherHandler(t);

            private bool TryDisposeOther()
            {
                if (Interlocked.Decrement(ref _otherDisposeWip) != 0)
                {
                    Dispose(_other);
                    return false;
                }
                return true;
            }

            private void OtherHandler(Task<bool> t)
            {
                if (t.IsFaulted)
                {
                    _otherError = ExceptionHelper.Extract(t.Exception);
                    _otherDone = true;
                    if (TryDisposeOther())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
                else if (t.Result)
                {
                    Interlocked.Exchange(ref _otherValue, _other.Current);
                    if (TryDisposeOther())
                    {
                        ResumeHelper.Resume(ref _resume);
                        MoveNextOther();
                    }
                }
                else
                {
                    _otherDone = true;
                    if (TryDisposeOther())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
            }

            private void Dispose(IAsyncDisposable disposable)
            {
                disposable.DisposeAsync()
                    .AsTask()
                    .ContinueWith(DisposeHandlerAction, this);
            }

            private static readonly Action<Task, object> DisposeHandlerAction = (t, state) => ((WithLatestFromEnumerator)state).DisposeHandler(t);

            private void DisposeHandler(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _disposeWip, ref _disposeError, _disposeTask);
            }
        }
    }
}
