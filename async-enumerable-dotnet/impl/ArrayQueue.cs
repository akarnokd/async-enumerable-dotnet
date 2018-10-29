// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("async-enumerable-dotnet-test")]

namespace async_enumerable_dotnet.impl
{
    internal struct ArrayQueue<T>
    {
        private int _producerIndex;
        private int _consumerIndex;

        private T[] _array;

        internal ArrayQueue(int capacity)
        {
            _producerIndex = 0;
            _consumerIndex = 0;
            _array = new T[capacity];
        }

        internal void Enqueue(T item)
        {
            var pi = _producerIndex;
            var ci = _consumerIndex;
            var a = _array;
            var len = a.Length;

            a[pi] = item;

            pi = (pi + 1) & (len - 1);
            if (pi == ci)
            {
                var b = new T[len * 2];
                Array.Copy(a, ci, b, 0, len - ci);
                Array.Copy(a, 0, b, len - ci, ci);
                _array = b;
                _consumerIndex = 0;
                _producerIndex = len;
            }
            else
            {
                _producerIndex = pi;
            }
        }

        internal bool Dequeue(out T item)
        {
            var pi = _producerIndex;
            var ci = _consumerIndex;
            var a = _array;
            var len = a.Length;

            if (pi == ci)
            {
                item = default;
                return false;
            }
            item = a[ci];
            a[ci] = default;
            _consumerIndex = (ci + 1) & (len - 1);
            return true;
        }

        internal void Release()
        {
            _array = null;
        }

        internal void ForEach<TState>(TState state, Action<T, TState> onEach)
        {
            var ci = _consumerIndex;
            var pi = _producerIndex;
            var a = _array;
            var len = a.Length;

            while (ci != pi)
            {
                onEach(a[ci], state);
                ci = (ci + 1) & (len - 1);
            }
        }
    }
}
