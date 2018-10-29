// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// Until C# 8: Dispose a resource or sequence asynchronously in an awaitable fashion.
    /// </summary>
    public interface IAsyncDisposable
    {
        /// <summary>
        /// Dispose a resource or sequence asynchronously.
        /// </summary>
        /// <returns>The task completed when the dispose action completes.</returns>
        ValueTask DisposeAsync();
    }

    /// <summary>
    /// Until C# 8: Represents an active asynchronous sequence which can be polled for more items.
    /// </summary>
    /// <typeparam name="T">The element type of the async sequence.</typeparam>
    /// <remarks>
    /// The <see cref="IAsyncDisposable.DisposeAsync"/> should be called exactly once and
    /// only after an outstanding <see cref="MoveNextAsync"/>'s task completed.
    /// </remarks>
    public interface IAsyncEnumerator<out T> : IAsyncDisposable
    {
        /// <summary>
        /// Request the next element from the async sequence.
        /// </summary>
        /// <returns>A task that returns true if the next item can be read via
        /// <see cref="Current"/>, false if there will be no further items.</returns>
        ValueTask<bool> MoveNextAsync();
        
        /// <summary>
        /// Returns the current item after a successful <see cref="MoveNextAsync"/> call.
        /// </summary>
        T Current { get; }
    }

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
