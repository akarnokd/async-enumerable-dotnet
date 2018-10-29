// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace async_enumerable_dotnet_test
{
    internal sealed class ListComparer<T> : IEqualityComparer<IList<T>>
    {
        internal static readonly ListComparer<T> Default = new ListComparer<T>();

        public bool Equals(IList<T> x, IList<T> y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(IList<T> obj)
        {
            var hash = 19;

            unchecked
            {
                hash = obj.Aggregate(hash, (current, t) => current * 31 + (t != null ? t.GetHashCode() : 0));
            }

            return hash;
        }
    }
}
