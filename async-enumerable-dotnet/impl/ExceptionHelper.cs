using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    /// <summary>
    /// Helper methods for aggregating multiple Exceptions atomically.
    /// </summary>
    internal static class ExceptionHelper
    {
        /// <summary>
        /// The singleton exception indicating a terminal state so
        /// that no further exceptions will be aggregated.
        /// </summary>
        internal static readonly Exception Terminated = new TerminatedException();

        /// <summary>
        /// Atomically aggregate the given exception into the target field
        /// or return false if the field contains the terminated exception indicator.
        /// </summary>
        /// <param name="field">The target field</param>
        /// <param name="ex">The exception to aggregate</param>
        /// <returns>True if successful, false if the field already has the terminated
        /// indicator.</returns>
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
                    b = ex;
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

        /// <summary>
        /// Atomically swap in the terminated indicator and return the
        /// previous exception (may be null if none).
        /// </summary>
        /// <param name="field">The target field.</param>
        /// <returns>The last exception or null if no exceptions were in the field.</returns>
        internal static Exception Terminate(ref Exception field)
        {
            return Interlocked.Exchange(ref field, Terminated);
        }

        /// <summary>
        /// An exception indicating a terminal state within an Exception field.
        /// </summary>
        private sealed class TerminatedException : Exception
        {
            internal TerminatedException() : base("No further exceptions.")
            {

            }
        }

        /// <summary>
        /// If the given exception is of an AggregateException with
        /// only a single inner exception, extract it.
        /// </summary>
        /// <param name="ex">The exception to un-aggregate</param>
        /// <returns>The inner solo exception or <paramref name="ex"/>.</returns>
        internal static Exception Unaggregate(Exception ex)
        {
            if (ex is AggregateException g && g.InnerExceptions.Count == 1)
            {
                return g.InnerExceptions[0];
            }
            return ex;
        }
    }
}
