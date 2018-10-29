using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// Provides API for generating items.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public interface IAsyncEmitter<in T>
    {
        /// <summary>
        /// Push an item and wait until the generator can proceed.
        /// </summary>
        /// <param name="value">The element to produce.</param>
        /// <returns>The task that should be awaited before calling the method again.</returns>
        ValueTask Next(T value);

        /// <summary>
        /// Returns true if the consumer requested stopping a sequence.
        /// </summary>
        bool DisposeAsyncRequested { get; }
    }

    /// <summary>
    /// The "push-side" of an IAsyncEnumerator.
    /// Each method should be awaited and called in a non-overlapping fashion.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// The protocol is <code>Next* (Error | Complete)?</code>
    /// </remarks>
    public interface IAsyncConsumer<in T>
    {
        /// <summary>
        /// Push a value. Can be called multiple times.
        /// </summary>
        /// <param name="value">The value to push.</param>
        /// <returns>The task to await before calling any of the methods again.</returns>
        ValueTask Next(T value);

        /// <summary>
        /// Push a final exception. Can be called at most once.
        /// </summary>
        /// <param name="ex">The exception to push.</param>
        /// <returns>The task to await before calling any of the methods again.</returns>
        ValueTask Error(Exception ex);

        /// <summary>
        /// Indicate no more items will be pushed. Can be called at most once.
        /// </summary>
        /// <returns>The task to await before calling any of the methods again.</returns>
        ValueTask Complete();
    }
}
