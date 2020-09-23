// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SwitchMap<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, IAsyncEnumerable<TResult>> _mapper;

        public SwitchMap(IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper)
        {
            _source = source;
            _mapper = mapper;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var sourceCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var en = new SwitchMapEnumerator(_source.GetAsyncEnumerator(sourceCTS.Token), _mapper, sourceCTS);
            en.MoveNext();
            return en;
        }

        private sealed class SwitchMapEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, IAsyncEnumerable<TResult>> _mapper;

            private readonly CancellationTokenSource _sourceCTS;

            private InnerHandler _current;

            private volatile bool _done;
            private Exception _error;

            public TResult Current { get; private set; }

            private TaskCompletionSource<bool> _resume;

            private int _sourceWip;
            private int _sourceDisposeWip;

            private int _allDisposeWip;
            private Exception _allDisposeError;
            private readonly TaskCompletionSource<bool> _disposeTask;

            private static readonly InnerHandler DisposedInnerHandler = new InnerHandler(null, null, null);

            public SwitchMapEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper, CancellationTokenSource cts)
            {
                _source = source;
                _mapper = mapper;
                _disposeTask = new TaskCompletionSource<bool>();
                _sourceCTS = cts;
                Volatile.Write(ref _allDisposeWip, 1);
            }

            public ValueTask DisposeAsync()
            {
                _sourceCTS.Cancel();

                if (Interlocked.Increment(ref _sourceDisposeWip) == 1)
                {
                    Dispose(_source);
                }

                Interlocked.Exchange(ref _current, DisposedInnerHandler)?.Dispose();

                return new ValueTask(_disposeTask.Task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = _done;
                    var curr = Volatile.Read(ref _current);

                    if (d && curr == null)
                    {
                        var ex = _error;
                        if (ex != null)
                        {
                            _error = null;
                            throw ex;
                        }
                        return false;
                    }

                    if (curr != null)
                    {
                        d = curr.Done;
                        if (curr.HasValue)
                        {
                            Current = curr.Value;
                            curr.Value = default;
                            curr.HasValue = false;
                            if (curr == Volatile.Read(ref _current))
                            {
                                curr.MoveNext();
                            }
                            return true;
                        }
                        if (d)
                        {
                            if (Interlocked.CompareExchange(ref _current, null, curr) == curr)
                            {
                                curr.Dispose();
                            }
                            continue;
                        }
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            internal void MoveNext()
            {
                QueueDrainHelper.MoveNext(_source, ref _sourceWip, ref _sourceDisposeWip, NextHandlerAction, this);
            }

            private static readonly Action<Task<bool>, object> NextHandlerAction = (t, state) => ((SwitchMapEnumerator)state).NextHandler(t);

            private bool TryDispose()
            {
                if (Interlocked.Decrement(ref _sourceDisposeWip) != 0)
                {
                    Dispose(_source);
                    return false;
                }
                return true;
            }

            private void NextHandler(Task<bool> t)
            {
                if (t.IsCanceled)
                {
                    ExceptionHelper.AddException(ref _error, new OperationCanceledException());
                    _done = true;
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
                else if (t.IsFaulted)
                {
                    ExceptionHelper.AddException(ref _error, ExceptionHelper.Extract(t.Exception));
                    _done = true;
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
                else if (t.Result)
                {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(_sourceCTS.Token);
                    IAsyncEnumerator<TResult> src;
                    try
                    {
                        src = _mapper(_source.Current).GetAsyncEnumerator(cts.Token);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.AddException(ref _error, ex);
                        _done = true;
                        Dispose(_source);
                        Signal();
                        return;
                    }

                    if (TryDispose())
                    {
                        Interlocked.Increment(ref _allDisposeWip);
                        var inner = new InnerHandler(src, this, cts);

                        for (; ; )
                        {
                            var curr = Volatile.Read(ref _current);
                            if (curr == DisposedInnerHandler)
                            {
                                inner.Dispose();
                                break;
                            }
                            if (Interlocked.CompareExchange(ref _current, inner, curr) == curr)
                            {
                                curr?.Dispose();
                                inner.MoveNext();
                                MoveNext();
                                return;
                            }
                        }
                    }
                }
                else
                {
                    _done = true;
                    if (TryDispose())
                    {
                        Signal();
                    }
                }
            }

            private void Dispose(IAsyncDisposable disposable)
            {
                disposable.DisposeAsync()
                    .AsTask()
                    .ContinueWith(DisposeHandlerAction, this);
            }

            private static readonly Action<Task, object> DisposeHandlerAction = (t, state) => ((SwitchMapEnumerator)state).DisposeHandler(t);

            private void DisposeHandler(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _allDisposeWip, ref _allDisposeError, _disposeTask);
            }

            private void InnerError(InnerHandler sender, Exception ex)
            {
                ExceptionHelper.AddException(ref _error, ex);
                sender.Done = true;
                Signal();
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }

            private sealed class InnerHandler
            {
                private readonly IAsyncEnumerator<TResult> _source;

                private readonly SwitchMapEnumerator _parent;

                private readonly CancellationTokenSource _cts;

                private int _disposeWip;

                private int _sourceWip;

                internal TResult Value;
                internal volatile bool HasValue;
                internal volatile bool Done;

                public InnerHandler(IAsyncEnumerator<TResult> source, SwitchMapEnumerator parent, CancellationTokenSource cts)
                {
                    _source = source;
                    _parent = parent;
                    _cts = cts;
                }

                internal void MoveNext()
                {
                    QueueDrainHelper.MoveNext(_source, ref _sourceWip, ref _disposeWip, InnerNextHandlerAction, this);
                }

                private static readonly Action<Task<bool>, object> InnerNextHandlerAction = (t, state) => ((InnerHandler)state).Next(t);

                internal void Dispose()
                {
                    _cts.Cancel();
                    if (Interlocked.Increment(ref _disposeWip) == 1)
                    {
                        _parent.Dispose(_source);
                    }
                }

                private bool TryDispose()
                {
                    if (Interlocked.Decrement(ref _disposeWip) != 0)
                    {
                        _parent.Dispose(_source);
                        return false;
                    }
                    return true;
                }

                private void Next(Task<bool> t)
                {
                    if (t.IsCanceled)
                    {
                        if (TryDispose())
                        {
                            _parent.InnerError(this, new OperationCanceledException());
                        }
                    }
                    else if (t.IsFaulted)
                    {
                        if (TryDispose())
                        {
                            _parent.InnerError(this, ExceptionHelper.Extract(t.Exception));
                        }
                    }
                    else if (t.Result)
                    {
                        Value = _source.Current;
                        HasValue = true;
                        if (TryDispose())
                        {
                            _parent.Signal();
                        }
                    }
                    else
                    {
                        Done = true;
                        if (TryDispose())
                        {
                            _parent.Signal();
                        }
                    }
                }
            }
        }
    }
}
