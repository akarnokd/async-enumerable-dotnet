using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("async-enumerable-dotnet-test")]

namespace async_enumerable_dotnet.impl
{
    internal struct ArrayQueue<T>
    {
        int producerIndex;
        int consumerIndex;

        T[] array;

        internal ArrayQueue(int capacity)
        {
            producerIndex = 0;
            consumerIndex = 0;
            array = new T[capacity];
        }

        internal void Enqueue(T item)
        {
            var pi = producerIndex;
            var ci = consumerIndex;
            var a = array;
            var len = a.Length;

            a[pi] = item;

            pi = (pi + 1) & (len - 1);
            if (pi == ci)
            {
                var b = new T[len * 2];
                Array.Copy(a, ci, b, 0, len - ci);
                Array.Copy(a, 0, b, len - ci, ci);
                array = b;
                consumerIndex = 0;
                producerIndex = len;
            }
            else
            {
                producerIndex = pi;
            }
        }

        internal bool Dequeue(out T item)
        {
            var pi = producerIndex;
            var ci = consumerIndex;
            var a = array;
            var len = a.Length;

            if (pi == ci)
            {
                item = default;
                return false;
            }
            item = a[ci];
            a[ci] = default;
            consumerIndex = (ci + 1) & (len - 1);
            return true;
        }

        internal void Release()
        {
            array = null;
        }

        internal void ForEach<U>(U state, Action<T, U> onEach)
        {
            var ci = consumerIndex;
            var pi = producerIndex;
            var a = array;
            var len = a.Length;

            while (ci != pi)
            {
                onEach(a[ci], state);
                ci = (ci + 1) & (len - 1);
            }
        }
    }
}
