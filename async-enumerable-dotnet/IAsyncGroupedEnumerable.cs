// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// An IAsyncEnumerable with a key property to support grouping operations.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The element type of the async sequence.</typeparam>
    public interface IAsyncGroupedEnumerable<out TKey, TValue> : IAsyncEnumerable<TValue>
    {
        /// <summary>
        /// Returns the group key.
        /// </summary>
        // ReSharper disable once UnusedMemberInSuper.Global
        TKey Key
        {
            // ReSharper disable once UnusedMemberInSuper.Global
            get;
        }
    }
}
