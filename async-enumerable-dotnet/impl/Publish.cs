// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Publish<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<IAsyncEnumerable<TSource>, IAsyncEnumerable<TResult>> _func;

        public Publish(IAsyncEnumerable<TSource> source, Func<IAsyncEnumerable<TSource>, IAsyncEnumerable<TResult>> func)
        {
            _source = source;
            _func = func;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            var subject = new MulticastAsyncEnumerable<TSource>();
            IAsyncEnumerable<TResult> result;
            try
            {
                result = _func(subject);
            }
            catch (Exception ex)
            {
                return new Error<TResult>.ErrorEnumerator(ex);
            }
            var en = new PublishEnumerator(_source.GetAsyncEnumerator(), subject, result.GetAsyncEnumerator());
            return en;
        }

        private sealed class PublishEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            internal readonly MulticastAsyncEnumerable<TSource> _subject;

            internal readonly IAsyncEnumerator<TResult> _result;

            public TResult Current => _result.Current;

            private int _sourceDisposeWip;

            private int _sourceWip;

            private int _disposeWip;
            private Exception _disposeError;
            private readonly TaskCompletionSource<bool> _disposeTask;

            private bool _once;

            internal PublishEnumerator(IAsyncEnumerator<TSource> source, MulticastAsyncEnumerable<TSource> subject, IAsyncEnumerator<TResult> result)
            {
                _source = source;
                _subject = subject;
                _result = result;
                _disposeTask = new TaskCompletionSource<bool>();
                Volatile.Write(ref _disposeWip, 2);
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref _sourceDisposeWip) == 1)
                {
                    Dispose(_source);
                }

                Dispose(_result);

                return new ValueTask(_disposeTask.Task);
            }

            public ValueTask<bool> MoveNextAsync()
            {
                ValueTask<bool> t = _result.MoveNextAsync();

                if (!_once)
                {
                    _once = true;
                    // We have to start polling the source now
                    // So that the func has time to subscribe
                    MoveNextSource();
                }
                return t;
            }

            internal void MoveNextSource()
            {
                if (Interlocked.Increment(ref _sourceWip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref _sourceDisposeWip) == 1)
                        {
                            _source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(HandleSourceAction, this);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref _sourceWip) != 0);
                }
            }

            private static readonly Func<Task<bool>, object, Task> HandleSourceAction =
                (t, state) => ((PublishEnumerator)state).HandleSource(t);
            
            private async Task HandleSource(Task<bool> t)
            {
                if (t.IsFaulted)
                {
                    if (TryDisposeSource())
                    {
                        await _subject.Error(t.Exception);
                    }
                }
                else if (t.Result)
                {
                    var v = _source.Current;
                    if (TryDisposeSource())
                    {
                        await _subject.Next(v);

                        MoveNextSource();
                    }
                }
                else
                {
                    if (TryDisposeSource())
                    {
                        await _subject.Complete();
                    }
                }
            }

            bool TryDisposeSource()
            {
                if (Interlocked.Decrement(ref _sourceDisposeWip) != 0)
                {
                    Dispose(_source);
                    return false;
                }
                return true;
            }

            private void Dispose(IAsyncDisposable disposable)
            {
                _source.DisposeAsync()
                    .AsTask()
                    .ContinueWith(HandleDisposeAction, this);

            }

            private static readonly Action<Task, object> HandleDisposeAction = (t, state) => ((PublishEnumerator)state).HandleDispose(t);

            void HandleDispose(Task t)
            {
                if (t.IsFaulted)
                {
                    ExceptionHelper.AddException(ref _disposeError, t.Exception);
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
        }
    }
}
