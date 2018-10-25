using System;
using System.Collections.Generic;
using System.Text;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// An IAsyncEnumerable with a key property to support grouping operations.
    /// </summary>
    /// <typeparam name="K">The key type.</typeparam>
    /// <typeparam name="V">The element type of the async sequence.</typeparam>
    public interface IAsyncGroupedEnumerable<K, V> : IAsyncEnumerable<V>
    {
        /// <summary>
        /// Returns the group key.
        /// </summary>
        K Key { get; }
    }
}
