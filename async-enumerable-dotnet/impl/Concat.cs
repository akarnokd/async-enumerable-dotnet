// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Concat<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T>[] _sources;

        public Concat(IAsyncEnumerable<T>[] sources)
        {
            _sources = sources;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ConcatEnumerator(_sources);
        }

        private sealed class ConcatEnumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerable<T>[] _sources;

            private int _index;

            private IAsyncEnumerator<T> _current;

            public ConcatEnumerator(IAsyncEnumerable<T>[] sources)
            {
                _sources = sources;
            }

            public T Current => _current.Current;

            public ValueTask DisposeAsync()
            {
                if (_current != null)
                {
                    return _current.DisposeAsync();
                }
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (_current == null)
                    {
                        var idx = _index;
                        if (idx == _sources.Length)
                        {
                            return false;
                        }

                        _index = idx + 1;
                        _current = _sources[idx].GetAsyncEnumerator();
                    }

                    if (await _current.MoveNextAsync())
                    {
                        return true;
                    }

                    await _current.DisposeAsync();
                    _current = null;
                }
            }
        }
    }

    internal sealed class ConcatEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<IAsyncEnumerable<T>> _sources;

        public ConcatEnumerable(IEnumerable<IAsyncEnumerable<T>> sources)
        {
            _sources = sources;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ConcatEnumerator(_sources.GetEnumerator());
        }

        private sealed class ConcatEnumerator : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<IAsyncEnumerable<T>> _sources;

            private IAsyncEnumerator<T> _current;

            public ConcatEnumerator(IEnumerator<IAsyncEnumerable<T>> sources)
            {
                _sources = sources;
            }

            public T Current => _current.Current;

            public ValueTask DisposeAsync()
            {
                _sources.Dispose();
                if (_current != null)
                {
                    return _current.DisposeAsync();
                }
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (_current == null)
                    {
                        if (_sources.MoveNext())
                        {
                            _current = _sources.Current.GetAsyncEnumerator();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (await _current.MoveNextAsync())
                    {
                        return true;
                    }

                    await _current.DisposeAsync();
                    _current = null;
                }
            }
        }
    }
}
