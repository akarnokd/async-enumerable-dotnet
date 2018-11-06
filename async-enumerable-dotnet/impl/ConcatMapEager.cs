// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ConcatMapEager<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TSource, IAsyncEnumerable<TResult>> _mapper;

        private readonly int _maxConcurrency;

        private readonly int _prefetch;

        public ConcatMapEager(IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper, int maxConcurrency, int prefetch)
        {
            _source = source;
            _mapper = mapper;
            _maxConcurrency = maxConcurrency;
            _prefetch = prefetch;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            var en = new ConcatMapEagerEnumerator(_source.GetAsyncEnumerator(), _mapper, _maxConcurrency, _prefetch);
            en.MoveNextSource();
            return en;
        }

        private sealed class ConcatMapEagerEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly Func<TSource, IAsyncEnumerable<TResult>> _mapper;

            private readonly int _prefetch;

            private int _sourceOutstanding;

            private readonly ConcurrentQueue<InnerHandler> _inners;
            private volatile bool _sourceDone;
            private Exception _error;

            private volatile bool _disposeRequested;

            private InnerHandler _currentInner;

            public TResult Current { get; private set; }

            private int _sourceWip;
            private int _sourceDisposeWip;

            private TaskCompletionSource<bool> _resume;

            private int _disposeWip;
            private Exception _disposeError;
            private readonly TaskCompletionSource<bool> _disposeTask;

            public ConcatMapEagerEnumerator(IAsyncEnumerator<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> mapper, int maxConcurrency, int prefetch)
            {
                _source = source;
                _mapper = mapper;
                _prefetch = prefetch;
                _sourceOutstanding = maxConcurrency;
                _disposeWip = 1;
                _inners = new ConcurrentQueue<InnerHandler>();
                _disposeTask = new TaskCompletionSource<bool>();
            }

            public ValueTask DisposeAsync()
            {
                _disposeRequested = true;
                if (Interlocked.Increment(ref _sourceDisposeWip) == 1)
                {
                    Dispose(_source);
                }
                _currentInner?.Dispose();
                _currentInner = null;
                while (_inners.TryDequeue(out var inner))
                {
                    inner.Dispose();
                }

                return new ValueTask(_disposeTask.Task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = _sourceDone;
                    var curr = _currentInner;
                    if (curr == null)
                    {
                        if (_inners.TryDequeue(out curr))
                        {
                            _currentInner = curr;
                        }
                    }

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
                        var success = curr.Queue.TryDequeue(out var v);

                        if (d && !success)
                        {
                            curr.Dispose();
                            _currentInner = null;
                            SourceConsumedOne();
                            continue;
                        }

                        if (success)
                        {
                            Current = v;
                            curr.ConsumedOne();
                            return true;
                        }
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            private void SourceConsumedOne()
            {
                if (Interlocked.Increment(ref _sourceOutstanding) == 1)
                {
                    MoveNextSource();
                }
            }

            internal void MoveNextSource()
            {
                QueueDrainHelper.MoveNext(_source, ref _sourceWip, ref _sourceDisposeWip, NextHandlerAction, this);
            }

            private static readonly Action<Task<bool>, object> NextHandlerAction = (t, state) => ((ConcatMapEagerEnumerator)state).NextHandler(t);

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
                if (t.IsFaulted)
                {
                    ExceptionHelper.AddException(ref _error, ExceptionHelper.Extract(t.Exception));
                    _sourceDone = true;
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref _resume);
                    }
                }
                else if (t.Result)
                {
                    IAsyncEnumerator<TResult> src;
                    try
                    {
                        src = _mapper(_source.Current).GetAsyncEnumerator();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.AddException(ref _error, ex);
                        _sourceDone = true;
                        src = null;
                        if (TryDispose())
                        {
                            ResumeHelper.Resume(ref _resume);
                            return;
                        }
                    }

                    if (src != null)
                    {
                        var inner = new InnerHandler(src, this);
                        Interlocked.Increment(ref _disposeWip);
                        _inners.Enqueue(inner);

                        if (_disposeRequested)
                        {
                            while (_inners.TryDequeue(out var inner2))
                            {
                                inner2.Dispose();
                            }
                            return;
                        }

                        if (TryDispose())
                        {
                            inner.MoveNext();
                            if (Interlocked.Decrement(ref _sourceOutstanding) != 0)
                            {
                                MoveNextSource();
                            }
                            ResumeHelper.Resume(ref _resume);
                        }
                    }
                }
                else
                {
                    _sourceDone = true;
                    if (TryDispose())
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

            private static readonly Action<Task, object> DisposeHandlerAction = (t, state) => ((ConcatMapEagerEnumerator)state).DisposeHandler(t);

            private void DisposeHandler(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _disposeWip, ref _disposeError, _disposeTask);
            }

            private sealed class InnerHandler
            {
                private readonly IAsyncEnumerator<TResult> _source;

                private readonly ConcatMapEagerEnumerator _parent;

                internal readonly ConcurrentQueue<TResult> Queue;

                internal volatile bool Done;

                private int _wip;
                private int _disposeWip;

                private int _outstanding;
                private readonly int _limit;

                private int _consumed;

                public InnerHandler(IAsyncEnumerator<TResult> source, ConcatMapEagerEnumerator parent)
                {
                    _source = source;
                    _parent = parent;
                    Queue = new ConcurrentQueue<TResult>();
                    var p = parent._prefetch;
                    _outstanding = p;
                    _limit = p - (p >> 2);
                }

                internal void MoveNext()
                {
                    QueueDrainHelper.MoveNext(_source, ref _wip, ref _disposeWip, InnerNextHandlerAction, this);
                }

                private static readonly Action<Task<bool>, object> InnerNextHandlerAction = (t, state) => ((InnerHandler)state).InnerNextHandler(t);

                internal void Dispose()
                {
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

                private void InnerNextHandler(Task<bool> t)
                {
                    if (t.IsFaulted)
                    {
                        ExceptionHelper.AddException(ref _parent._error, ExceptionHelper.Extract(t.Exception));
                        Done = true;
                        if (TryDispose())
                        {
                            ResumeHelper.Resume(ref _parent._resume);
                        }
                    }
                    else if (t.Result)
                    {
                        Queue.Enqueue(_source.Current);
                        if (TryDispose())
                        {
                            if (Interlocked.Decrement(ref _outstanding) != 0)
                            {
                                MoveNext();
                            }
                            ResumeHelper.Resume(ref _parent._resume);
                        }
                    }
                    else
                    {
                        Done = true;
                        if (TryDispose())
                        {
                            ResumeHelper.Resume(ref _parent._resume);
                        }
                    }
                }

                internal void ConsumedOne()
                {
                    var c = _consumed + 1;
                    if (c == _limit)
                    {
                        _consumed = 0;
                        if (Interlocked.Add(ref _outstanding, c) == c)
                        {
                            MoveNext();
                        }
                    }
                    else
                    {
                        _consumed = c;
                    }
                }
            }
        }
    }
}
