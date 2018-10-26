using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    /// <summary>
    /// Utility class to work with CancellationTokenSources atomically.
    /// </summary>
    internal static class CancellationHelper
    {
        /// <summary>
        /// The cancelled indicator, do not leak!
        /// </summary>
        internal static readonly CancellationTokenSource Cancelled = new CancellationTokenSource();

        /// <summary>
        /// Atomically replace an old CancellationTokenSource within the field
        /// or cancel the new token source if the field contains the Cancelled indicator.
        /// </summary>
        /// <param name="field">The target field</param>
        /// <param name="cts">The new cancellation token source to swap in.</param>
        /// <returns>True if successful, false if the field contains the Cancelled indicator</returns>
        internal static bool Replace(ref CancellationTokenSource field, CancellationTokenSource cts)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref field);
                if (a == Cancelled)
                {
                    cts?.Cancel();
                    return false;
                }
                if (Interlocked.CompareExchange(ref field, cts, a) == a)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Atomically swap in the Cancelled indicator and cancel the
        /// current token source if any.
        /// </summary>
        /// <param name="field">The target field.</param>
        /// <returns>True if the cancel happened</returns>
        internal static bool Cancel(ref CancellationTokenSource field)
        {
            if (Volatile.Read(ref field) != Cancelled)
            {
                var old = Interlocked.Exchange(ref field, Cancelled);
                if (old != Cancelled)
                {
                    old?.Cancel();
                    return true;
                }
            }
            return false;
        }
    }
}
