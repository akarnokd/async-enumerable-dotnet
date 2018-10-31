// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.


namespace async_enumerable_dotnet.impl
{
    /// <summary>
    /// Hosts the EmptyIndicator singleton.
    /// </summary>
    internal static class EmptyHelper
    {
        internal static readonly object EmptyIndicator = new object();
    }
}
