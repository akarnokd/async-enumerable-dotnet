using System;
using System.Collections.Generic;
using System.Text;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Defer<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEnumerable<T>> func;

        public Defer(Func<IAsyncEnumerable<T>> func)
        {
            this.func = func;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = default(IAsyncEnumerator<T>);
            try
            {
                en = func().GetAsyncEnumerator();
            }
            catch (Exception ex)
            {
                en = new Error<T>.ErrorEnumerator(ex);
            }
            return en;
        }
    }
}
