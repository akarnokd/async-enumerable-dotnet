// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// Until C# 8: Represents a deferred/lazy asynchronous sequence of values.
    /// </summary>
    /// <typeparam name="T">The element type of the async sequence.</typeparam>
    public interface IAsyncEnumerable<out T>
    {
        /// <summary>
        /// Returns an <see cref="IAsyncEnumerator{T}"/> representing an active asynchronous sequence.
        /// </summary>
        /// <returns>The active asynchronous sequence to be consumed.</returns>
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }
}
