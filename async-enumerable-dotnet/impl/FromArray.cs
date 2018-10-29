// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FromArray<T> : IAsyncEnumerable<T>
    {
        private readonly T[] _values;

        public FromArray(T[] values)
        {
            _values = values;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromArrayEnumerator(_values);
        }

        private sealed class FromArrayEnumerator : IAsyncEnumerator<T>
        {
            private readonly T[] _values;

            private int _index;

            public T Current => _values[_index];

            public FromArrayEnumerator(T[] values)
            {
                _values = values;
                _index = -1;
            }

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                var idx = _index + 1;
                if (idx < _values.Length)
                {
                    _index = idx;
                    return new ValueTask<bool>(true);
                }
                return new ValueTask<bool>(false);
            }
        }
    }
}
