﻿// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Merge<TSource> : IAsyncEnumerable<TSource>
    {
        private readonly IAsyncEnumerable<TSource>[] _sources;

        public Merge(IAsyncEnumerable<TSource>[] sources)
        {
            _sources = sources;
        }

        public IAsyncEnumerator<TSource> GetAsyncEnumerator()
        {
            return new MergeEnumerator(_sources);
        }

        private sealed class MergeEnumerator : IAsyncEnumerator<TSource>
        {
            private readonly InnerHandler[] _sources;

            public TSource Current { get; private set; }

            private readonly ConcurrentQueue<Entry> _queue;
            private Exception _error;
            private int _done;

            private TaskCompletionSource<bool> _resume;

            private int _disposeWip;
            private Exception _disposeError;
            private readonly TaskCompletionSource<bool> _disposeTask;

            private bool _once;

            public MergeEnumerator(IAsyncEnumerable<TSource>[] sources)
            {
                var n = sources.Length;
                _sources = new InnerHandler[n];
                for (var i = 0; i < sources.Length; i++)
                {
                    _sources[i] = new InnerHandler(sources[i].GetAsyncEnumerator(), this);
                }
                _queue = new ConcurrentQueue<Entry>();
                _disposeTask = new TaskCompletionSource<bool>();
                _disposeWip = n;
                Volatile.Write(ref _done, n);
            }

            public ValueTask DisposeAsync()
            {
                foreach(var inner in _sources)
                {
                    inner.Dispose();
                }
                return new ValueTask(_disposeTask.Task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (!_once)
                {
                    _once = true;
                    foreach (var inner in _sources)
                    {
                        inner.MoveNext();
                    }
                }
                for (; ; )
                {
                    var d = Volatile.Read(ref _done) == 0;
                    var success = _queue.TryDequeue(out var v);

                    if (d && !success)
                    {
                        var ex = _error;
                        if (ex != null)
                        {
                            _error = null;
                            throw ex;
                        }
                        return false;
                    }

                    if (success)
                    {
                        Current = v.Value;
                        v.Sender.MoveNext();
                        return true;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            private void Dispose(IAsyncDisposable disposable)
            {
                disposable.DisposeAsync()
                    .AsTask()
                    .ContinueWith(DisposeHandlerAction, this);
            }

            private static readonly Action<Task, object> DisposeHandlerAction = (t, state) => ((MergeEnumerator)state).DisposeHandler(t);

            void DisposeHandler(Task t)
            {
                if (t.IsFaulted)
                {
                    ExceptionHelper.AddException(ref _disposeError, ExceptionHelper.Extract(t.Exception));
                }
                if (Interlocked.Decrement(ref _disposeWip) == 0)
                {
                    var ex = _disposeError;
                    if (ex != null)
                    {
                        _disposeError = null;
                        _disposeTask.TrySetException(ex);
                    }
                    else
                    {
                        _disposeTask.TrySetResult(true);
                    }
                }
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }

            internal void InnerNext(InnerHandler sender, TSource item)
            {
                _queue.Enqueue(new Entry { Sender = sender, Value = item });
                Signal();
            }

            internal void InnerError(InnerHandler sender, Exception ex)
            {
                ExceptionHelper.AddException(ref _error, ex);
                Interlocked.Decrement(ref _done);
                Signal();
            }

            internal void InnerComplete(InnerHandler sender)
            {
                Interlocked.Decrement(ref _done);
                Signal();
            }

            private struct Entry
            {
                internal TSource Value;
                internal InnerHandler Sender;
            }

            internal sealed class InnerHandler
            {
                private readonly IAsyncEnumerator<TSource> _source;

                private readonly MergeEnumerator _parent;

                private int _disposeWip;

                private int _wip;

                private bool _done;

                public InnerHandler(IAsyncEnumerator<TSource> source, MergeEnumerator parent)
                {
                    _source = source;
                    _parent = parent;
                }

                internal void Dispose()
                {
                    if (Interlocked.Increment(ref _disposeWip) == 1)
                    {
                        _parent.Dispose(_source);
                    }
                }

                internal void MoveNext()
                {
                    if (_done)
                    {
                        return;
                    }
                    if (Interlocked.Increment(ref _wip) == 1)
                    {
                        do
                        {
                            if (Interlocked.Increment(ref _disposeWip) == 1)
                            {
                                _source.MoveNextAsync()
                                    .AsTask()
                                    .ContinueWith(NextHandlerAction, this);
                            }
                            else
                            {
                                break;
                            }
                        }
                        while (Interlocked.Decrement(ref _wip) != 0);
                    }
                }

                private static readonly Action<Task<bool>, object> NextHandlerAction = (t, state) => ((InnerHandler)state).Next(t);

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
                    if (t.IsFaulted)
                    {
                        _done = true;
                        if (TryDispose())
                        {
                            _parent.InnerError(this, ExceptionHelper.Extract(t.Exception));
                        }
                    }
                    else if (t.Result)
                    {
                        var v = _source.Current;
                        if (TryDispose())
                        {
                            _parent.InnerNext(this, v);
                        }
                    }
                    else
                    {
                        _done = true;
                        if (TryDispose())
                        {
                            _parent.InnerComplete(this);
                        }
                    }

                }
            }
        }
    }
}