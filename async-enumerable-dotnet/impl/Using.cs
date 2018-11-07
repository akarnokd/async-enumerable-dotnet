// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Using<TSource, TResource> : IAsyncEnumerable<TSource>
    {
        private readonly Func<TResource> _resourceProvider;

        private readonly Func<TResource, IAsyncEnumerable<TSource>> _sourceProvider;

        private readonly Action<TResource> _resourceCleanup;

        public Using(Func<TResource> resourceProvider, Func<TResource, IAsyncEnumerable<TSource>> sourceProvider, Action<TResource> resourceCleanup)
        {
            _resourceProvider = resourceProvider;
            _sourceProvider = sourceProvider;
            _resourceCleanup = resourceCleanup;
        }

        public IAsyncEnumerator<TSource> GetAsyncEnumerator()
        {
            TResource resource;
            try
            {
                resource = _resourceProvider();
            }
            catch (Exception ex)
            {
                return new Error<TSource>.ErrorEnumerator(ex);
            }
            IAsyncEnumerable<TSource> source;
            try
            {
                source = _sourceProvider(resource);
            } 
            catch (Exception ex)
            {
                try
                {
                    _resourceCleanup(resource);
                }
                catch (Exception exc)
                {
                    return new Error<TSource>.ErrorEnumerator(new AggregateException(ex, exc));
                }
                return new Error<TSource>.ErrorEnumerator(ex);
            }

            return new UsingEnumerator(source.GetAsyncEnumerator(), resource, _resourceCleanup);
        }

        private sealed class UsingEnumerator : IAsyncEnumerator<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;

            private readonly TResource _resource;

            private readonly Action<TResource> _resourceCleanup;

            public UsingEnumerator(IAsyncEnumerator<TSource> source, TResource resource, Action<TResource> resourceCleanup)
            {
                _source = source;
                _resource = resource;
                _resourceCleanup = resourceCleanup;
            }

            public TSource Current => _source.Current;

            public async ValueTask DisposeAsync()
            {
                var error = default(Exception);
                try
                {
                    _resourceCleanup(_resource);
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                try
                {
                    await _source.DisposeAsync();
                }
                catch (Exception ex2)
                {
                    error = error == null ? ex2 : new AggregateException(error, ex2);
                }

                if (error != null)
                {
                    throw error;
                }
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return _source.MoveNextAsync();
            }
        }
    }
}
