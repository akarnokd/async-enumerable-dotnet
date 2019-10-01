// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Debounce<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly TimeSpan _delay;

        private readonly bool _emitLast;

        public Debounce(IAsyncEnumerable<T> source, TimeSpan delay, bool emitLast)
        {
            _source = source;
            _delay = delay;
            _emitLast = emitLast;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var en = new DebounceEnumerator(_source.GetAsyncEnumerator(cancellationToken), _delay, _emitLast, cancellationToken);
            en.MoveNext();
            return en;
        }

        private sealed class DebounceEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private readonly TimeSpan _delay;

            private readonly bool _emitLast;

            private readonly CancellationToken _ct;

            public T Current { get; private set; }

            private int _disposeWip;

            private TaskCompletionSource<bool> _disposeTask;

            private int _sourceWip;

            private TaskCompletionSource<bool> _resume;

            private volatile bool _done;
            private Exception _error;

            private Node _latest;

            private T _emitLastItem;

            private long _sourceIndex;

            private CancellationTokenSource _cts;

            public DebounceEnumerator(IAsyncEnumerator<T> source, TimeSpan delay, bool emitLast, CancellationToken ct)
            {
                _source = source;
                _delay = delay;
                _emitLast = emitLast;
                _ct = ct;
            }

            public ValueTask DisposeAsync()
            {
                CancellationHelper.Cancel(ref _cts);
                if (Interlocked.Increment(ref _disposeWip) == 1)
                {
                    if (_emitLast)
                    {
                        _emitLastItem = default;
                    }
                    return _source.DisposeAsync();
                }
                return ResumeHelper.Await(ref _disposeTask);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = _done;
                    var v = Interlocked.Exchange(ref _latest, null);

                    if (d && v == null)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }

                    if (v != null)
                    {
                        Current = v.Value;
                        return true;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            internal void MoveNext()
            {
                QueueDrainHelper.MoveNext(_source, ref _sourceWip, ref _disposeWip, MainHandlerAction, this);
            }

            private static readonly Action<Task<bool>, object> MainHandlerAction =
                (t, state) => ((DebounceEnumerator) state).HandleMain(t);
            
            private bool TryDispose()
            {
                if (Interlocked.Decrement(ref _disposeWip) != 0)
                {
                    if (_emitLast)
                    {
                        _emitLastItem = default;
                    }
                    ResumeHelper.Complete(ref _disposeTask, _source.DisposeAsync());
                    return false;
                }
                return true;
            }

            private void HandleMain(Task<bool> t)
            {
                if (t.IsCanceled)
                {
                    _error = new OperationCanceledException();
                    _done = true;
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
                else if (t.IsFaulted)
                {
                    CancellationHelper.Cancel(ref _cts);
                    if (_emitLast)
                    {
                        var idx = _sourceIndex;
                        if (idx != 0)
                        {
                            SetLatest(_emitLastItem, idx + 1);
                            _emitLastItem = default;
                        }
                    }
                    _error = ExceptionHelper.Extract(t.Exception);
                    _done = true;
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
                else if (t.Result)
                {
                    Volatile.Read(ref _cts)?.Cancel();

                    var v = _source.Current;
                    if (TryDispose())
                    {
                        if (_emitLast)
                        {
                            _emitLastItem = v;
                        }
                        var idx = ++_sourceIndex;
                        var newCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
                        if (CancellationHelper.Replace(ref _cts, newCts))
                        {
                            Task.Delay(_delay, newCts.Token)
                                .ContinueWith(tt => TimerHandler(tt, v, idx), newCts.Token);
                            MoveNext();
                        }
                    }
                }
                else
                {
                    CancellationHelper.Cancel(ref _cts);
                    if (_emitLast)
                    {
                        var idx = _sourceIndex;
                        if (idx != 0)
                        {
                            SetLatest(_emitLastItem, idx + 1);
                            _emitLastItem = default;
                        }
                    }
                    _done = true;
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
            }

            private void TimerHandler(Task t, T value, long idx)
            {
                if (!t.IsCanceled && SetLatest(value, idx))
                {
                    ResumeHelper.Resume(ref _resume);
                }
            }

            private bool SetLatest(T value, long idx)
            {
                var b = default(Node);
                for (; ; )
                {
                    var a = Volatile.Read(ref _latest);
                    if (a == null || a.Index < idx)
                    {
                        if (b == null)
                        {
                            b = new Node(idx, value);
                        }
                        if (Interlocked.CompareExchange(ref _latest, b, a) == a)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            private sealed class Node
            {
                internal readonly long Index;
                internal readonly T Value;

                public Node(long index, T value)
                {
                    Index = index;
                    Value = value;
                }
            }
        }
    }
}
