// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class BufferBoundaryExact<TSource, TOther, TCollection> : IAsyncEnumerable<TCollection> where TCollection : ICollection<TSource>
    {

        private readonly IAsyncEnumerable<TSource> _source;

        private readonly Func<TCollection> _collectionSupplier;

        private readonly int _maxSize;

        private readonly IAsyncEnumerable<TOther> _boundary;

        public BufferBoundaryExact(IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> boundary, 
            Func<TCollection> collectionSupplier, int maxSize)
        {
            _source = source;
            _boundary = boundary;
            _collectionSupplier = collectionSupplier;
            _maxSize = maxSize;
        }

        public IAsyncEnumerator<TCollection> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var mainCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var otherCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var en = new BufferBoundaryExactEnumerator(_source.GetAsyncEnumerator(mainCancel.Token), 
                _boundary.GetAsyncEnumerator(otherCancel.Token), _collectionSupplier, _maxSize, mainCancel, otherCancel);

            en.MoveNextOther();
            en.MoveNextSource();
            return en;
        }

        private sealed class BufferBoundaryExactEnumerator : IAsyncEnumerator<TCollection>
        {

            private readonly IAsyncEnumerator<TSource> _source;

            private readonly IAsyncEnumerator<TOther> _other;

            private readonly Func<TCollection> _collectionSupplier;

            private readonly int _maxSize;

            private readonly CancellationTokenSource _mainCancel;

            private readonly CancellationTokenSource _otherCancel;

            private int _sourceWip;
            private int _sourceDisposeWip;

            private int _otherWip;
            private int _otherDisposeWip;

            private bool _done;
            private Exception _error;

            private readonly ConcurrentQueue<Entry> _queue;
            private TaskCompletionSource<bool> _resume;

            private int _disposeWip;
            private Exception _disposeError;
            private readonly TaskCompletionSource<bool> _disposeTask;

            private TCollection _buffer;
            private int _size;

            private bool _suppressCancel;

            public TCollection Current { get; private set; }

            public BufferBoundaryExactEnumerator(IAsyncEnumerator<TSource> source, 
                IAsyncEnumerator<TOther> other, Func<TCollection> collectionSupplier, int maxSize,
                CancellationTokenSource mainCancel, CancellationTokenSource otherCancel)
            {
                _source = source;
                _other = other;
                _collectionSupplier = collectionSupplier;
                _maxSize = maxSize;
                _queue = new ConcurrentQueue<Entry>();
                _disposeTask = new TaskCompletionSource<bool>();
                _mainCancel = mainCancel;
                _otherCancel = otherCancel;
                Volatile.Write(ref _disposeWip, 2);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_done)
                {
                    var ex = ExceptionHelper.Terminate(ref _error);
                    if (ex != null)
                    {
                        throw ex;
                    }
                    return false;
                }
                for (; ;)
                {
                    var success = _queue.TryDequeue(out var entry);

                    if (success)
                    {
                        var b = _buffer;

                        if (entry.Done)
                        {
                            _done = true;

                            if (b != null)
                            {
                                Current = b;
                                _buffer = default;
                                return true;
                            }

                            var ex = ExceptionHelper.Terminate(ref _error);
                            if (ex != null)
                            {
                                throw ex;
                            }
                            return false;
                        }
                        if (entry.Boundary)
                        {
                            if (b == null)
                            {
                                Current = _collectionSupplier();
                            }
                            else
                            {
                                Current = b;
                                _buffer = default;
                            }
                            _size = 0;
                            MoveNextOther();
                            return true;
                        }

                        if (b == null)
                        {
                            b = _collectionSupplier();
                            _buffer = b;
                        }
                        b.Add(entry.Value);
                        if (++_size == _maxSize)
                        {
                            Current = b;
                            _buffer = default;
                            _size = 0;
                            MoveNextSource();
                            return true;
                        }

                        MoveNextSource();
                        continue;
                    }

                    await ResumeHelper.Await(ref _resume);
                    ResumeHelper.Clear(ref _resume);
                }
            }

            public async ValueTask DisposeAsync()
            {
                _mainCancel.Cancel();
                _otherCancel.Cancel();
                if (Interlocked.Increment(ref _sourceDisposeWip) == 1)
                {
                    Dispose(_source);
                }
                if (Interlocked.Increment(ref _otherDisposeWip) == 1)
                {
                    Dispose(_other);
                }
                await _disposeTask.Task;

                Current = default;
                while (_queue.TryDequeue(out _)) { }
            }

            internal void MoveNextSource()
            {
                QueueDrainHelper.MoveNext(_source, ref _sourceWip, ref _sourceDisposeWip, HandleNextSourceAction, this);
            }

            private static readonly Action<Task<bool>, object> HandleNextSourceAction = (t, state) => ((BufferBoundaryExactEnumerator)state).HandleNextSource(t);

            private bool TryDisposeSource()
            {
                if (Interlocked.Decrement(ref _sourceDisposeWip) != 0)
                {
                    Dispose(_source);
                    return false;
                }
                return true;
            }

            private void HandleNextSource(Task<bool> t)
            {
                if (t.IsCanceled)
                {
                    if (!Volatile.Read(ref _suppressCancel))
                    {
                        ExceptionHelper.AddException(ref _error, new OperationCanceledException());
                        _queue.Enqueue(new Entry
                        {
                            Done = true
                        });
                        Volatile.Write(ref _suppressCancel, true);
                        _otherCancel.Cancel();
                    }
                }
                else if (t.IsFaulted)
                {
                    ExceptionHelper.AddException(ref _error, ExceptionHelper.Extract(t.Exception));
                    _queue.Enqueue(new Entry
                    {
                        Done = true
                    });
                    Volatile.Write(ref _suppressCancel, true);
                    _otherCancel.Cancel();
                }
                else if (t.Result)
                {
                    _queue.Enqueue(new Entry
                    {
                        Value = _source.Current
                    });
                }
                else
                {
                    _queue.Enqueue(new Entry
                    {
                        Done = true
                    });
                    Volatile.Write(ref _suppressCancel, true);
                    _otherCancel.Cancel();
                }
                if (TryDisposeSource())
                {
                    ResumeHelper.Resume(ref _resume);
                }
            }

            internal void MoveNextOther()
            {
                QueueDrainHelper.MoveNext(_other, ref _otherWip, ref _otherDisposeWip, HandleNextOtherAction, this);
            }

            private static readonly Action<Task<bool>, object> HandleNextOtherAction = (t, state) => ((BufferBoundaryExactEnumerator)state).HandleNextOther(t);

            private bool TryDisposeOther()
            {
                if (Interlocked.Decrement(ref _otherDisposeWip) != 0)
                {
                    Dispose(_other);
                    return false;
                }
                return true;
            }

            private void HandleNextOther(Task<bool> t)
            {
                if (t.IsCanceled)
                {
                    if (!Volatile.Read(ref _suppressCancel))
                    {
                        ExceptionHelper.AddException(ref _error, new OperationCanceledException());
                        _queue.Enqueue(new Entry
                        {
                            Done = true
                        });
                        Volatile.Write(ref _suppressCancel, true);
                        _mainCancel.Cancel();
                    }
                }
                else if (t.IsFaulted)
                {
                    ExceptionHelper.AddException(ref _error, ExceptionHelper.Extract(t.Exception));
                    _queue.Enqueue(new Entry
                    {
                        Done = true
                    });
                    Volatile.Write(ref _suppressCancel, true);
                    _mainCancel.Cancel();
                }
                else if (t.Result)
                {
                    _queue.Enqueue(new Entry
                    {
                        Boundary = true
                    });
                }
                else
                {
                    _queue.Enqueue(new Entry
                    {
                        Done = true
                    });
                    Volatile.Write(ref _suppressCancel, true);
                    _mainCancel.Cancel();
                }
                if (TryDisposeOther())
                {
                    ResumeHelper.Resume(ref _resume);
                }
            }

            private void Dispose(IAsyncDisposable disposable)
            {
                disposable.DisposeAsync()
                    .AsTask()
                    .ContinueWith(DisposeHandlerAction, this);
            }

            private static readonly Action<Task, object> DisposeHandlerAction = (t, state) => ((BufferBoundaryExactEnumerator)state).DisposeHandler(t);

            private void DisposeHandler(Task t)
            {
                QueueDrainHelper.DisposeHandler(t, ref _disposeWip, ref _disposeError, _disposeTask);
            }

            private struct Entry
            {
                internal bool Boundary;
                internal bool Done;
                internal TSource Value;
            }
        }
    }
}
