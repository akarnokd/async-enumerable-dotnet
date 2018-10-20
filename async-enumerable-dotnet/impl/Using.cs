using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Using<T, R> : IAsyncEnumerable<T>
    {
        readonly Func<R> resourceProvider;

        readonly Func<R, IAsyncEnumerable<T>> sourceProvider;

        readonly Action<R> resourceCleanup;

        public Using(Func<R> resourceProvider, Func<R, IAsyncEnumerable<T>> sourceProvider, Action<R> resourceCleanup)
        {
            this.resourceProvider = resourceProvider;
            this.sourceProvider = sourceProvider;
            this.resourceCleanup = resourceCleanup;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var resource = default(R);
            try
            {
                resource = resourceProvider();
            }
            catch (Exception ex)
            {
                return new Error<T>.ErrorEnumerator(ex);
            }
            var source = default(IAsyncEnumerable<T>);
            try
            {
                source = sourceProvider(resource);
            } catch (Exception ex)
            {
                try
                {
                    resourceCleanup(resource);
                }
                catch (Exception exc)
                {
                    return new Error<T>.ErrorEnumerator(new AggregateException(ex, exc));
                }
                return new Error<T>.ErrorEnumerator(ex);
            }

            return new UsingEnumerator(source.GetAsyncEnumerator(), resource, resourceCleanup);
        }

        internal sealed class UsingEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly R resource;

            readonly Action<R> resourceCleanup;

            public UsingEnumerator(IAsyncEnumerator<T> source, R resource, Action<R> resourceCleanup)
            {
                this.source = source;
                this.resource = resource;
                this.resourceCleanup = resourceCleanup;
            }

            public T Current => source.Current;

            public async ValueTask DisposeAsync()
            {
                var error = default(Exception);
                try
                {
                    resourceCleanup(resource);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                await source.DisposeAsync();
                if (error != null)
                {
                    throw error;
                }
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return source.MoveNextAsync();
            }
        }
    }
}
