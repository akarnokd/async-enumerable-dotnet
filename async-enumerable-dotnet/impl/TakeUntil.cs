// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class TakeUntil<TSource, TOther> : IAsyncEnumerable<TSource>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly IAsyncEnumerable<TOther> _other;

        public TakeUntil(IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> other)
        {
            _source = source;
            _other = other;
        }

        public IAsyncEnumerator<TSource> GetAsyncEnumerator()
        {
            var en = new TakeUntilEnumerator(_source.GetAsyncEnumerator(), _other.GetAsyncEnumerator());
            en.MoveNextOther();
            return en;
        }

        private sealed class TakeUntilEnumerator : IAsyncEnumerator<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly IAsyncEnumerator<TOther> _other;

            private readonly TaskCompletionSource<bool> _disposeReady;

            public TSource Current => _source.Current;

            private TaskCompletionSource<bool> _currentTask;

            private Exception _otherError;

            private int _disposeMain;

            private int _disposeOther;

            private int _disposed;

            private Exception _disposeException;

            public TakeUntilEnumerator(IAsyncEnumerator<TSource> source, IAsyncEnumerator<TOther> other)
            {
                _source = source;
                _other = other;
                _disposeReady = new TaskCompletionSource<bool>();
            }

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

                await _disposeReady.Task;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var task = Volatile.Read(ref _currentTask);
                    if (task == TakeUntilHelper.UntilTask)
                    {
                        if (_otherError != null)
                        {
                            return new ValueTask<bool>(Task.FromException<bool>(_otherError));
                        }
                        return new ValueTask<bool>(false);
                    }
                    var newTask = new TaskCompletionSource<bool>();
                    if (Interlocked.CompareExchange(ref _currentTask, newTask, task) == task)
                    {
                        if (Interlocked.Increment(ref _disposeMain) == 1)
                        {
                            _source.MoveNextAsync().AsTask()
                                .ContinueWith(t => HandleMain(t, newTask));
                            return new ValueTask<bool>(newTask.Task);
                        }
                    }
                }
            }

            private void HandleMain(Task<bool> t, TaskCompletionSource<bool> newTask)
            {
                if (Interlocked.Decrement(ref _disposeMain) != 0)
                {
                    Dispose(_source);
                }
                else if (t.IsFaulted)
                {
                    newTask.TrySetException(t.Exception);
                }
                else
                {
                    newTask.TrySetResult(t.Result);
                }
            }

            internal void MoveNextOther()
            {
                Interlocked.Increment(ref _disposeOther);
                _other.MoveNextAsync().AsTask()
                    .ContinueWith(HandleOtherAction, this, TaskContinuationOptions.ExecuteSynchronously);
            }

            private static readonly Action<Task<bool>, object> HandleOtherAction =
                (task, state) => ((TakeUntilEnumerator) state).HandleOther(task);
            
            private void HandleOther(Task t)
            {
                if (Interlocked.Decrement(ref _disposeOther) != 0)
                {
                    Dispose(_other);
                }
                else if (t.IsFaulted) {
                    _otherError = t.Exception;
                    var oldTask = Interlocked.Exchange(ref _currentTask, TakeUntilHelper.UntilTask);
                    if (oldTask != TakeUntilHelper.UntilTask)
                    {
                        oldTask?.TrySetException(t.Exception);
                    }
                }
                else
                {
                    var oldTask = Interlocked.Exchange(ref _currentTask, TakeUntilHelper.UntilTask);
                    if (oldTask != TakeUntilHelper.UntilTask)
                    {
                        if (Interlocked.Increment(ref _disposeMain) == 1)
                        {
                            Dispose(_source);
                        }
                        if (Interlocked.Increment(ref _disposeOther) == 1)
                        {
                            Dispose(_other);
                        }
                        oldTask?.TrySetResult(false);
                    }
                }
            }

            private void Dispose(IAsyncDisposable en)
            {
                en.DisposeAsync().AsTask()
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            ExceptionHelper.AddException(ref _disposeException, t.Exception);
                        }
                        if (Interlocked.Increment(ref _disposed) == 2)
                        {
                            var ex = _disposeException;
                            if (ex != null)
                            {
                                _disposeReady.TrySetException(ex);
                            }
                            else
                            {
                                _disposeReady.TrySetResult(false);
                            }
                        }
                    });
            }
        }
    }

    /// <summary>
    /// Hosts the singleton UntilTask indicator
    /// </summary>
    internal static class TakeUntilHelper
    {
        internal static readonly TaskCompletionSource<bool> UntilTask = new TaskCompletionSource<bool>();
    }
}
