// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Defer<T> : IAsyncEnumerable<T>
    {
        private readonly Func<IAsyncEnumerable<T>> _func;

        public Defer(Func<IAsyncEnumerable<T>> func)
        {
            _func = func;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            IAsyncEnumerator<T> en;
            try
            {
                en = _func().GetAsyncEnumerator(cancellationToken);
            }
            catch (Exception ex)
            {
                en = new Error<T>.ErrorEnumerator(ex);
            }
            return en;
        }
    }
}
