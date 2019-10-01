// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Latest<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        public Latest(IAsyncEnumerable<T> source)
        {
            _source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new LatestEnumerator(_source.GetAsyncEnumerator());
            en.MoveNext();
            return en;
        }

        private sealed class LatestEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _source;

            private int _disposeWip;

            private TaskCompletionSource<bool> _disposeTask;

            private TaskCompletionSource<bool> _resumeTask;

            private Exception _error;
            private volatile bool _done;

            private object _latest;

            private int _consumerWip;

            public T Current { get; private set; }

            public LatestEnumerator(IAsyncEnumerator<T> source)
            {
                _source = source;
                Volatile.Write(ref _latest, EmptyHelper.EmptyIndicator);
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref _disposeWip) == 1)
                {
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

                    await ResumeHelper.Await(ref _resumeTask);
                    ResumeHelper.Clear(ref _resumeTask);
                }
            }

            internal void MoveNext()
            {
                QueueDrainHelper.MoveNext(_source, ref _consumerWip, ref _disposeWip, MainHandlerAction, this);
            }

            private static readonly Action<Task<bool>, object> MainHandlerAction =
                (t, state) => ((LatestEnumerator) state).HandleMain(t);
            
            private bool TryDispose()
            {
                if (Interlocked.Decrement(ref _disposeWip) != 0)
                {
                    ResumeHelper.Complete(ref _disposeTask, _source.DisposeAsync());
                    return false;
                }
                return true;
            }

            private void HandleMain(Task<bool> t)
            {
                if (t.IsFaulted)
                {
                    _error = ExceptionHelper.Extract(t.Exception);
                    _done = true;
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref _resumeTask);
                    }
                }
                else if (t.Result)
                {
                    Interlocked.Exchange(ref _latest, _source.Current);
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref _resumeTask);
                    }
                    MoveNext();
                }
                else
                {
                    _done = true;
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref _resumeTask);
                    }
                }
            }
        }
    }
}
