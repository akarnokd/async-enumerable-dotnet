// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using async_enumerable_dotnet.impl;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// Delivers items or terminal events to multiple
    /// consumers.
    /// </summary>
    /// <typeparam name="T">The element type of the async sequence.</typeparam>
    public sealed class MulticastAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncConsumer<T>
    {
        private MulticastEnumerator[] _enumerators;

        private Exception _error;

        private static readonly MulticastEnumerator[] Empty = new MulticastEnumerator[0];

        private static readonly MulticastEnumerator[] Terminated = new MulticastEnumerator[0];

        /// <summary>
        /// Returns true if there are any consumers to this AsyncEnumerable.
        /// </summary>
        public bool HasConsumers => _enumerators.Length != 0;

        /// <summary>
        /// Construct a non-terminated MulticastAsyncEnumerable.
        /// </summary>
        public MulticastAsyncEnumerable()
        {
            Volatile.Write(ref _enumerators, Empty);
        }

        /// <summary>
        /// Push a value. Can be called multiple times.
        /// </summary>
        /// <param name="value">The value to push.</param>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public async ValueTask Next(T value)
        {
            foreach (var inner in Volatile.Read(ref _enumerators))
            {
                await inner.Next(value);
            }
        }

        /// <summary>
        /// Push a final exception. Can be called at most once.
        /// </summary>
        /// <param name="ex">The exception to push.</param>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public async ValueTask Error(Exception ex)
        {
            _error = ex;
            foreach (var inner in Interlocked.Exchange(ref _enumerators, Terminated))
            {
                await inner.Error(ex);
            }
        }

        /// <summary>
        /// Indicate no more items will be pushed. Can be called at most once.
        /// </summary>
        /// <returns>The task to await before calling any of the methods again.</returns>
        public async ValueTask Complete()
        {
            foreach (var inner in Interlocked.Exchange(ref _enumerators, Terminated))
            {
                await inner.Complete();
            }
        }

        /// <summary>
        /// Returns an <see cref="IAsyncEnumerator{T}"/> representing an active asynchronous sequence.
        /// </summary>
        /// <returns>The active asynchronous sequence to be consumed.</returns>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var en = new MulticastEnumerator(this);
            if (Add(en))
            {
                // FIXME what about the registration???
                cancellationToken.Register(v => (v as MulticastEnumerator).RemoveFromParent(), en);
                return en;
            }
            if (_error != null)
            {
                return new Error<T>.ErrorEnumerator(_error);
            }
            return new Empty<T>();
        }

        private bool Add(MulticastEnumerator inner)
        {
            for (; ;)
            {
                var a = Volatile.Read(ref _enumerators);
                if (a == Terminated)
                {
                    return false;
                }
                var n = a.Length;
                var b = new MulticastEnumerator[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = inner;
                if (Interlocked.CompareExchange(ref _enumerators, b, a) == a)
                {
                    return true;
                }
            }
        }

        private void Remove(MulticastEnumerator inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref _enumerators);
                var n = a.Length;
                if (n == 0)
                {
                    return;
                }

                var j = Array.IndexOf(a, inner);

                if (j < 0)
                {
                    return;
                }

                MulticastEnumerator[] b;
                if (n == 1)
                {
                    b = Empty;
                }
                else
                {
                    b = new MulticastEnumerator[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref _enumerators, b, a) == a)
                {
                    return;
                }
            }
        }

        private sealed class MulticastEnumerator : IAsyncEnumerator<T>, IAsyncConsumer<T>
        {
            private readonly MulticastAsyncEnumerable<T> _parent;

            private T _value;
            private bool _done;
            private Exception _error;

            private TaskCompletionSource<bool> _consumed;

            private TaskCompletionSource<bool> _valueReady;

            public T Current { get; private set; }

            public MulticastEnumerator(MulticastAsyncEnumerable<T> parent)
            {
                _parent = parent;
                var tcs = new TaskCompletionSource<bool>();
                tcs.TrySetResult(true);
                Volatile.Write(ref _consumed, tcs);
            }


            public async ValueTask Complete()
            {
                await ResumeHelper.Await(ref _consumed);
                ResumeHelper.Clear(ref _consumed);

                _done = true;

                ResumeHelper.Resume(ref _valueReady);
            }

            internal void RemoveFromParent()
            {
                _parent.Remove(this);
            }

            public ValueTask DisposeAsync()
            {
                Current = default;
                RemoveFromParent();
                // unblock any Next/Error/Complete waiting for consumption
                ResumeHelper.Resume(ref _consumed);
                return new ValueTask();
            }

            public async ValueTask Error(Exception ex)
            {
                await ResumeHelper.Await(ref _consumed);
                ResumeHelper.Clear(ref _consumed);

                _error = ex;

                ResumeHelper.Resume(ref _valueReady);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                await ResumeHelper.Await(ref _valueReady);
                ResumeHelper.Clear(ref _valueReady);

                if (_error != null)
                {
                    throw _error;
                }

                if (_done)
                {
                    return false;
                }

                Current = _value;
                _value = default;
                ResumeHelper.Resume(ref _consumed);
                return true;
            }

            public async ValueTask Next(T value)
            {
                await ResumeHelper.Await(ref _consumed);
                ResumeHelper.Clear(ref _consumed);

                _value = value;

                ResumeHelper.Resume(ref _valueReady);
            }
        }
    }
}
