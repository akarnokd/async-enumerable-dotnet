using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Range : IAsyncEnumerable<int>
    {
        readonly int start;

        readonly int end;

        public Range(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        public IAsyncEnumerator<int> GetAsyncEnumerator()
        {
            return new RangeEnumerator(start, end);
        }

        internal sealed class RangeEnumerator : IAsyncEnumerator<int>
        {
            readonly int end;

            int index;

            int current;

            public RangeEnumerator(int index, int end)
            {
                this.index = index;
                this.end = end;
            }

            public int Current => current;

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                var idx = index;
                if (idx == end)
                {
                    return new ValueTask<bool>(false);
                }
                index = idx + 1;
                current = idx;
                return new ValueTask<bool>(true);
            }
        }
    }
}
