// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    internal sealed class OnErrorResumeNext<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source;

        private readonly Func<Exception, IAsyncEnumerable<T>> _handler;

        public OnErrorResumeNext(IAsyncEnumerable<T> source, Func<Exception, IAsyncEnumerable<T>> handler)
        {
            _source = source;
            _handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new OnErrorResumeNextEnumerator(_source.GetAsyncEnumerator(cancellationToken), _handler, cancellationToken);
        }

        private sealed class OnErrorResumeNextEnumerator : IAsyncEnumerator<T>
        {
            private readonly CancellationToken _ct;

            private IAsyncEnumerator<T> _source;

            private Func<Exception, IAsyncEnumerable<T>> _handler;

            public OnErrorResumeNextEnumerator(IAsyncEnumerator<T> source, Func<Exception, IAsyncEnumerable<T>> handler,
                CancellationToken ct)
            {
                _source = source;
                _handler = handler;
                _ct = ct;
            }

            public T Current => _source.Current;

            public ValueTask DisposeAsync()
            {
                return _source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_handler == null)
                {
                    return await _source.MoveNextAsync();
                }

                try
                {
                    return await _source.MoveNextAsync();
                }
                catch (Exception ex)
                {
                    IAsyncEnumerator<T> en;

                    try
                    {
                        en = _handler(ex).GetAsyncEnumerator(_ct);
                    }
                    catch (Exception exc)
                    {
                        throw new AggregateException(ex, exc);
                    }

                    _handler = null;
                    _source = en;

                    return await en.MoveNextAsync();
                }
            }
        }
    }
}
