using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                foreach (var t in obj)
                {
                    hash = hash * 31 + (t != null ? t.GetHashCode() : 0);
                }
            }

            return hash;
        }
    }
}
