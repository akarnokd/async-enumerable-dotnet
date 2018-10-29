using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ZipArray<TSource, TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TSource>[] _sources;

        private readonly Func<TSource[], TResult> _zipper;

        public ZipArray(IAsyncEnumerable<TSource>[] sources, Func<TSource[], TResult> zipper)
        {
            _sources = sources;
            _zipper = zipper;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            var enumerators = new IAsyncEnumerator<TSource>[_sources.Length];
            for (var i = 0; i < _sources.Length; i++)
            {
                enumerators[i] = _sources[i].GetAsyncEnumerator();
            }
            return new ZipArrayEnumerator(enumerators, _zipper);
        }

        sealed class ZipArrayEnumerator : IAsyncEnumerator<TResult>
        {
            private readonly IAsyncEnumerator<TSource>[] _enumerators;

            private readonly Func<TSource[], TResult> _zipper;

            private readonly ValueTask<bool>[] _tasks;

            private bool _done;

            public ZipArrayEnumerator(IAsyncEnumerator<TSource>[] enumerators, Func<TSource[], TResult> zipper)
            {
                _enumerators = enumerators;
                _zipper = zipper;
                _tasks = new ValueTask<bool>[enumerators.Length];
            }

            public TResult Current { get; private set; }

            public async ValueTask DisposeAsync()
            {
                foreach (var en in _enumerators)
                {
                    await en.DisposeAsync().ConfigureAwait(false);
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_done)
                {
                    return false;
                }

                Current = default;

                var values = new TSource[_enumerators.Length];

                for (var i = 0; i < _enumerators.Length; i++)
                {
                    _tasks[i] = _enumerators[i].MoveNextAsync();
                }

                var fullRow = true;
                var errors = default(Exception);

                for (var i = 0; i < _tasks.Length; i++)
                {
                    try
                    {
                        if (await _tasks[i].ConfigureAwait(false))
                        {
                            values[i] = _enumerators[i].Current;
                        }
                        else
                        {
                            fullRow = false;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors = errors == null ? ex : new AggregateException(errors, ex);
                    }
                }

                if (!fullRow)
                {
                    _done = true;
                    if (errors != null)
                    {
                        throw errors;
                    }

                    return false;
                }

                Current = _zipper(values);
                return true;
            }
        }
    }
}
