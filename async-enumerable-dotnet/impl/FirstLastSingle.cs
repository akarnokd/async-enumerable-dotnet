using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class First<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly T defaultItem;

        readonly bool hasDefault;

        public First(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            this.source = source;
            this.defaultItem = defaultItem;
            this.hasDefault = hasDefault;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FirstEnumerator(source.GetAsyncEnumerator(), defaultItem, hasDefault);
        }

        internal sealed class FirstEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly T defaultItem;

            readonly bool hasDefault;

            T current;

            public T Current => current;

            bool done;

            public FirstEnumerator(IAsyncEnumerator<T> source, T defaultItem, bool hasDefault)
            {
                this.source = source;
                this.defaultItem = defaultItem;
                this.hasDefault = hasDefault;
            }

            public ValueTask DisposeAsync()
            {
                current = default;
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (done)
                {
                    return false;
                }
                done = true;

                if (await source.MoveNextAsync())
                {
                    current = source.Current;
                    return true;
                }
                if (hasDefault)
                {
                    current = defaultItem;
                    return true;
                }
                throw new IndexOutOfRangeException("The source is empty");
            }
        }
    }

    internal sealed class Last<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly T defaultItem;

        readonly bool hasDefault;

        public Last(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            this.source = source;
            this.defaultItem = defaultItem;
            this.hasDefault = hasDefault;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new LastEnumerator(source.GetAsyncEnumerator(), defaultItem, hasDefault);
        }

        internal sealed class LastEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly T defaultItem;

            readonly bool hasDefault;

            T current;

            public T Current => current;

            bool done;

            public LastEnumerator(IAsyncEnumerator<T> source, T defaultItem, bool hasDefault)
            {
                this.source = source;
                this.defaultItem = defaultItem;
                this.hasDefault = hasDefault;
            }

            public ValueTask DisposeAsync()
            {
                current = default;
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (done)
                {
                    return false;
                }
                done = true;
                var hasValue = false;
                var last = default(T); 
                while (await source.MoveNextAsync())
                {
                    hasValue = true;
                    last = source.Current;
                }

                if (hasValue)
                {
                    current = last;
                    return true;
                }
                if (hasDefault)
                {
                    current = defaultItem;
                    return true;
                }
                throw new IndexOutOfRangeException("The source is empty");
            }
        }
    }

    internal sealed class Single<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly T defaultItem;

        readonly bool hasDefault;

        public Single(IAsyncEnumerable<T> source, T defaultItem, bool hasDefault)
        {
            this.source = source;
            this.defaultItem = defaultItem;
            this.hasDefault = hasDefault;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new SingleEnumerator(source.GetAsyncEnumerator(), defaultItem, hasDefault);
        }
        internal sealed class SingleEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly T defaultItem;

            readonly bool hasDefault;

            T current;

            public T Current => current;

            bool done;

            public SingleEnumerator(IAsyncEnumerator<T> source, T defaultItem, bool hasDefault)
            {
                this.source = source;
                this.defaultItem = defaultItem;
                this.hasDefault = hasDefault;
            }

            public ValueTask DisposeAsync()
            {
                current = default;
                return source.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (done)
                {
                    return false;
                }
                done = true;

                if (await source.MoveNextAsync())
                {
                    var single = source.Current;
                    if (await source.MoveNextAsync())
                    {
                        throw new IndexOutOfRangeException("The source has more than one item");
                    }
                    current = single;
                    return true;
                }

                if (hasDefault)
                {
                    current = defaultItem;
                    return true;
                }
                throw new IndexOutOfRangeException("The source is empty");
            }
        }
    }

}
