// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using async_enumerable_dotnet.impl;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// A push-pull adapter that allows exactly one consumer of its IAsyncEnumerator
    /// and buffers items until the consumer arrives.
    /// </summary>
    /// <typeparam name="TSource">The element type of the input and output.</typeparam>
    public sealed class UnicastAsyncEnumerable<TSource> : IAsyncEnumerable<TSource>, IAsyncConsumer<TSource>
    {
        private readonly ConcurrentQueue<TSource> _queue;

        private volatile bool _done;
        private Exception _error;

        private TaskCompletionSource<bool> _resume;

        private int _once;

        private volatile bool _disposed;

        /// <summary>
        /// Returns true if there is currently a consumer to this async sequence.
        /// </summary>
        public bool HasConsumers => Volatile.Read(ref _once) != 0 && !_disposed;

        /// <summary>
        /// Returns true if the consumer has stopped consuming this async sequence.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Constructs an empty UnicastAsyncEnumerable.
        /// </summary>
        public UnicastAsyncEnumerable()
        {
            _queue = new ConcurrentQueue<TSource>();
        }

        /// <summary>
        /// Indicate no more items will be pushed. Can be called at most once.
        /// </summary>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public ValueTask Complete()
        {
            if (!_done && !_disposed)
            {
                _done = true;
                ResumeHelper.Resume(ref _resume);
            }
            return new ValueTask();
        }

        /// <summary>
        /// Push a final exception. Can be called at most once.
        /// </summary>
        /// <param name="ex">The exception to push.</param>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public ValueTask Error(Exception ex)
        {
            if (!_done && !_disposed)
            {
                _error = ex;
                _done = true;
                ResumeHelper.Resume(ref _resume);
            }
            return new ValueTask();
        }

        /// <summary>
        /// Push a value. Can be called multiple times.
        /// </summary>
        /// <param name="value">The value to push.</param>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public ValueTask Next(TSource value)
        {
            if (!_done && !_disposed)
            {
                _queue.Enqueue(value);
                ResumeHelper.Resume(ref _resume);
            }
            return new ValueTask();
        }

        /// <summary>
        /// Returns an <see cref="IAsyncEnumerator{T}"/> representing an active asynchronous sequence.
        /// </summary>
        /// <returns>The active asynchronous sequence to be consumed.</returns>
        public IAsyncEnumerator<TSource> GetAsyncEnumerator()
        {
            if (Interlocked.CompareExchange(ref _once, 1, 0) == 0)
            {
                return new UnicastEnumerator(this);
            }
            return new Error<TSource>.ErrorEnumerator(new InvalidOperationException("The UnicastAsyncEnumerable has its only allowed consumer already"));
        }

        private sealed class UnicastEnumerator : IAsyncEnumerator<TSource>
        {
            private UnicastAsyncEnumerable<TSource> _parent;

            public UnicastEnumerator(UnicastAsyncEnumerable<TSource> parent)
            {
                _parent = parent;
            }

            public TSource Current { get; private set; }

            public ValueTask DisposeAsync()
            {
                _parent._disposed = true;
                _parent = null;
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = _parent._done;
                    var success = _parent._queue.TryDequeue(out var v);

                    if (d && !success)
                    {
                        var ex = _parent._error;
                        if (ex != null)
                        {
                            throw ex;
                        }
                        return false;
                    }

                    if (success)
                    {
                        Current = v;
                        return true;
                    }

                    await ResumeHelper.Await(ref _parent._resume);
                    ResumeHelper.Clear(ref _parent._resume);
                }
            }
        }
    }
}
