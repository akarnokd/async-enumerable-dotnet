// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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

            // ReSharper disable once SuggestBaseTypeForParameter
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

            private void DisposeHandler(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _disposeWip, ref _disposeError, _disposeTask);
            }

            private void Signal()
            {
                ResumeHelper.Resume(ref _resume);
            }

            private void InnerNext(InnerHandler sender, TSource item)
            {
                _queue.Enqueue(new Entry { Sender = sender, Value = item });
                Signal();
            }

            private void InnerError(Exception ex)
            {
                ExceptionHelper.AddException(ref _error, ex);
                Interlocked.Decrement(ref _done);
                Signal();
            }

            private void InnerComplete()
            {
                Interlocked.Decrement(ref _done);
                Signal();
            }

            private struct Entry
            {
                internal TSource Value;
                internal InnerHandler Sender;
            }

            private sealed class InnerHandler
            {
                private readonly IAsyncEnumerator<TSource> _source;

                private readonly MergeEnumerator _parent;

                private int _disposeWip;

                private int _wip;

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
                    QueueDrainHelper.MoveNext(_source, ref _wip, ref _disposeWip, NextHandlerAction, this);
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
                        if (TryDispose())
                        {
                            _parent.InnerError(ExceptionHelper.Extract(t.Exception));
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
                        if (TryDispose())
                        {
                            _parent.InnerComplete();
                        }
                    }

                }
            }
        }
    }
}
