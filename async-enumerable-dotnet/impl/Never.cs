// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Never<T> : IAsyncEnumerable<T>
    {
        internal static readonly Never<T> Instance = new Never<T>();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new NeverEnumerator(cancellationToken);
        }

        private sealed class NeverEnumerator : IAsyncEnumerator<T>
        {
            private readonly CancellationToken _ct;

            public T Current => default;

            internal NeverEnumerator(CancellationToken ct)
            {
                _ct = ct;
            }

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                var tcs = new TaskCompletionSource<T>();
                using var reg = _ct.Register(t => (t as TaskCompletionSource<T>).TrySetCanceled(), tcs);
                
                await tcs.Task;

                return false;
            }
        }
    }
}
