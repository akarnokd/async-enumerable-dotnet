using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    /// <summary>
    /// Utility methods for working with resumption-type
    /// lock-free algorithms using TaskCompletionSources.
    /// </summary>
    internal static class ResumeHelper
    {
        /// <summary>
        /// Atomically get or create a TaskCompletionSource stored
        /// in the given field.
        /// </summary>
        /// <typeparam name="U">The element type of the completion source.</typeparam>
        /// <param name="resume">The field to store the completion source</param>
        /// <returns>The existing or created TaskCompletionSource.</returns>
        internal static TaskCompletionSource<U> Resume<U>(ref TaskCompletionSource<U> resume)
        {
            var b = default(TaskCompletionSource<U>);
            for (; ;)
            {
                var a = Volatile.Read(ref resume);
                if (a == null)
                {
                    if (b == null)
                    {
                        b = new TaskCompletionSource<U>();
                    }
                    if (Interlocked.CompareExchange(ref resume, b, a) == a)
                    {
                        return b;
                    }
                }
                else
                {
                    return a;
                }
            }
        }

        /// <summary>
        /// Atomically clear the target TaskCompletionSource field.
        /// </summary>
        /// <typeparam name="U">The element type of the completion source.</typeparam>
        /// <param name="resume">The field to clear.</param>
        internal static void Clear<U>(ref TaskCompletionSource<U> resume)
        {
            Interlocked.Exchange(ref resume, null);
        }

    }
}
