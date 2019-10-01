// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Empty<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
    {
        internal static readonly Empty<T> Instance = new Empty<T>();

        public T Current => default;

        public ValueTask DisposeAsync()
        {
            return new ValueTask();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return this;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(false);
        }
    }
}
