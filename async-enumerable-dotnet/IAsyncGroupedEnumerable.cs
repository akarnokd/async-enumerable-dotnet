namespace async_enumerable_dotnet
{
    /// <summary>
    /// An IAsyncEnumerable with a key property to support grouping operations.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The element type of the async sequence.</typeparam>
    public interface IAsyncGroupedEnumerable<out TKey, out TValue> : IAsyncEnumerable<TValue>
    {
        /// <summary>
        /// Returns the group key.
        /// </summary>
        TKey Key { get; }
    }
}
