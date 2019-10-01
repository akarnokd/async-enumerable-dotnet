// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class MulticastEnumerator<TSource, TResult> : IAsyncEnumerator<TResult>
    {
        private readonly IAsyncEnumerator<TSource> _source;

        private readonly IAsyncConsumer<TSource> _subject;

        private readonly IAsyncEnumerator<TResult> _result;

        public TResult Current => _result.Current;

        private int _sourceDisposeWip;

        private int _sourceWip;

        private int _disposeWip;
        private Exception _disposeError;
        private readonly TaskCompletionSource<bool> _disposeTask;

        private bool _once;

        internal MulticastEnumerator(IAsyncEnumerator<TSource> source, IAsyncConsumer<TSource> subject, IAsyncEnumerator<TResult> result)
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
            var t = _result.MoveNextAsync();

            if (!_once)
            {
                _once = true;
                // We have to start polling the source now
                // So that the func has time to subscribe
                MoveNextSource();
            }
            return t;
        }

        private void MoveNextSource()
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
            (t, state) => ((MulticastEnumerator<TSource, TResult>)state).HandleSource(t);

        private async Task HandleSource(Task<bool> t)
        {
            if (t.IsFaulted)
            {
                if (TryDisposeSource())
                {
                    await _subject.Error(ExceptionHelper.Extract(t.Exception));
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

        private bool TryDisposeSource()
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
            disposable.DisposeAsync()
                .AsTask()
                .ContinueWith(HandleDisposeAction, this);

        }

        private static readonly Action<Task, object> HandleDisposeAction = (t, state) => ((MulticastEnumerator<TSource, TResult>)state).HandleDispose(t);

        private void HandleDispose(Task t)
        {
            QueueDrainHelper.DisposeHandler(t, ref _disposeWip, ref _disposeError, _disposeTask);
        }
    }
}
