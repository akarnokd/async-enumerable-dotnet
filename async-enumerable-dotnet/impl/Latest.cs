// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

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

            private readonly Action<Task<bool>> _mainHandler;

            public LatestEnumerator(IAsyncEnumerator<T> source)
            {
                _source = source;
                _mainHandler = HandleMain;
                Volatile.Write(ref _latest, LatestHelper.EmptyIndicator);
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
                    var v = Interlocked.Exchange(ref _latest, LatestHelper.EmptyIndicator);
                    if (d && v == LatestHelper.EmptyIndicator)
                    {
                        if (_error != null)
                        {
                            throw _error;
                        }
                        return false;
                    }

                    if (v != LatestHelper.EmptyIndicator)
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
                if (Interlocked.Increment(ref _consumerWip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref _disposeWip) == 1)
                        {
                            _source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(_mainHandler);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref _consumerWip) != 0);
                }
            }

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

    /// <summary>
    /// Hosts the EmptyIndicator singleton.
    /// </summary>
    internal static class LatestHelper
    {
        internal static readonly object EmptyIndicator = new object();
    }
}
