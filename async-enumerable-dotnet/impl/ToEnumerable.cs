using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ToEnumerable<T> : IEnumerable<T>
    {

        readonly IAsyncEnumerable<T> source;

        public ToEnumerable(IAsyncEnumerable<T> source)
        {
            this.source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ToEnumerator(source.GetAsyncEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal sealed class ToEnumerator : IEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            public ToEnumerator(IAsyncEnumerator<T> source)
            {
                this.source = source;
            }

            public T Current => source.Current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                source.DisposeAsync().AsTask().Wait();
            }

            public bool MoveNext()
            {
                return source.MoveNextAsync().AsTask().Result;
            }

            public void Reset()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
