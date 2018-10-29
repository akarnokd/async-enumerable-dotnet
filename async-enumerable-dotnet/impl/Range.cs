// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Range : IAsyncEnumerable<int>
    {
        private readonly int _start;

        private readonly int _end;

        public Range(int start, int end)
        {
            _start = start;
            _end = end;
        }

        public IAsyncEnumerator<int> GetAsyncEnumerator()
        {
            return new RangeEnumerator(_start, _end);
        }

        private sealed class RangeEnumerator : IAsyncEnumerator<int>
        {
            private readonly int _end;

            private int _index;

            public RangeEnumerator(int index, int end)
            {
                _index = index;
                _end = end;
            }

            public int Current { get; private set; }

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                var idx = _index;
                if (idx == _end)
                {
                    return new ValueTask<bool>(false);
                }
                _index = idx + 1;
                Current = idx;
                return new ValueTask<bool>(true);
            }
        }
    }
}
