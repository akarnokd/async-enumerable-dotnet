using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    /// <summary>
    /// Helper methods for aggregating multiple Exceptions atomically.
    /// </summary>
    internal sealed class ExceptionHelper
    {
        /// <summary>
        /// The singleton exception indicating a terminal state so
        /// that no further exceptions will be aggregated.
        /// </summary>
        internal static readonly Exception Terminated = new TerminatedException();

        internal static bool AddException(ref Exception field, Exception ex)
        {
            for (; ;)
            {
                var a = Volatile.Read(ref field);
                if (a == Terminated)
                {
                    return false;
                }
                var b = default(Exception);
                if (a == null)
                {
                    b = a;
                }
                else if (a is AggregateException g)
                {
                    var list = new List<Exception>(g.InnerExceptions);
                    list.Add(ex);
                    b = new AggregateException(list);
                }
                else
                {
                    b = new AggregateException(a, ex);
                }
                if (Interlocked.CompareExchange(ref field, b, a) == a)
                {
                    return true;
                }
            }
        }

        internal static Exception Terminate(ref Exception field)
        {
            return Interlocked.Exchange(ref field, Terminated);
        }

        internal sealed class TerminatedException : Exception
        {
            internal TerminatedException() : base("No further exceptions.")
            {

            }
        }
    }
}
