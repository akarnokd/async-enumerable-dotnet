// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Never<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
    {
        internal static readonly Never<T> Instance = new Never<T>();

        public T Current => default;

        public ValueTask DisposeAsync()
        {
            return new ValueTask();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return this;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(new TaskCompletionSource<bool>().Task);
        }
    }
}
