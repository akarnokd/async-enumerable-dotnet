// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Amb<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T>[] _sources;

        public Amb(IAsyncEnumerable<T>[] sources)
        {
            _sources = sources;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new AmbEnumerator(_sources);
        }

        private sealed class AmbEnumerator : IAsyncEnumerator<T>
        {
            private readonly InnerHandler[] _sources;

            private readonly TaskCompletionSource<bool> _winTask;

            private InnerHandler _winner;

            private int _disposeWip;

            private readonly TaskCompletionSource<bool> _disposeTask;

            private Exception _disposeError;

            private bool _once;

            public T Current => _winner.Source.Current;

            // ReSharper disable once SuggestBaseTypeForParameter
            public AmbEnumerator(IAsyncEnumerable<T>[] sources)
            {
                var handlers = new InnerHandler[sources.Length];
                for (var i = 0; i < sources.Length; i++)
                {
                    handlers[i] = new InnerHandler(sources[i].GetAsyncEnumerator(), this);
                }
                _sources = handlers;
                _disposeWip = sources.Length;
                _disposeTask = new TaskCompletionSource<bool>();
                _winTask = new TaskCompletionSource<bool>();
            }

            public async ValueTask DisposeAsync()
            {
                _winner = null;
                foreach (var ih in _sources)
                {
                    ih.Dispose();
                }

                await _disposeTask.Task;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_once)
                {
                    return await _winner.Source.MoveNextAsync();
                }

                _once = true;

                foreach (var h in _sources)
                {
                    h.MoveNext(t => First(t, h));
                }

                var winnerHasValue = await _winTask.Task;

                foreach (var h in _sources)
                {
                    if (h != _winner)
                    {
                        h.Dispose();
                    }
                }

                return winnerHasValue;
            }

            private void First(Task<bool> task, InnerHandler sender)
            {
                if (sender.CheckDisposed())
                {
                    Dispose(sender.Source);
                }
                else
                {
                    if (Volatile.Read(ref _winner) == null && Interlocked.CompareExchange(ref _winner, sender, null) == null)
                    {
                        if (task.IsFaulted)
                        {
                            _winTask.TrySetException(ExceptionHelper.Extract(task.Exception));
                        }
                        else
                        {
                            _winTask.SetResult(task.Result);
                        }
                    }
                }
            }

            internal void Dispose(IAsyncDisposable d)
            {
                d.DisposeAsync()
                    .AsTask()
                    .ContinueWith(DisposeHandleAction, this);
            }

            private static readonly Action<Task, object> DisposeHandleAction =
                (t, state) => ((AmbEnumerator) state).DisposeHandle(t);

            private void DisposeHandle(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _disposeWip, ref _disposeError, _disposeTask);
            }
        }

        private sealed class InnerHandler
        {
            internal readonly IAsyncEnumerator<T> Source;

            private readonly AmbEnumerator _parent;

            private int _disposeWip;

            public InnerHandler(IAsyncEnumerator<T> source, AmbEnumerator parent)
            {
                Source = source;
                _parent = parent;
            }

            internal void Dispose()
            {
                if (Interlocked.Increment(ref _disposeWip) == 1)
                {
                    _parent.Dispose(Source);
                }
            }

            internal void MoveNext(Action<Task<bool>> handler)
            {
                if (Interlocked.Increment(ref _disposeWip) == 1)
                {
                    Source.MoveNextAsync()
                        .AsTask()
                        .ContinueWith(handler);
                }
            }

            internal bool CheckDisposed()
            {
                return Interlocked.Decrement(ref _disposeWip) != 0;
            }
        }
    }
}
