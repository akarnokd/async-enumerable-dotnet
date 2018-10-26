using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Clear<U>(ref TaskCompletionSource<U> resume)
        {
            Interlocked.Exchange(ref resume, null);
        }

        /// <summary>
        /// Atomically clear the target TaskCompletionSource field
        /// then atomically zero out the long field.
        /// </summary>
        /// <typeparam name="U">The element type of the completion source.</typeparam>
        /// <param name="resume">The field to clear.</param>
        /// <param name="wip">The field to zero out.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Clear<U>(ref TaskCompletionSource<U> resume, ref long wip)
        {
            Interlocked.Exchange(ref resume, null);
            Interlocked.Exchange(ref wip, 0L);
        }

        /// <summary>
        /// Atomically increments the wip counter and if the transition was from
        /// zero to one, it creates/sets the given task completion source to successful
        /// completion.
        /// </summary>
        /// <param name="wip">The work-in-progress indicator field.</param>
        /// <param name="resume">The resumption task completion source field.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Signal(ref long wip, ref TaskCompletionSource<bool> resume)
        {
            if (Interlocked.Increment(ref wip) == 1)
            {
                Resume(ref resume).TrySetResult(true);
            }
        }

        /// <summary>
        /// Create an action that takes a Task and sets the given
        /// TaskCompletionSource to the same state.
        /// </summary>
        /// <typeparam name="T">The element type of the source task</typeparam>
        /// <param name="tcs">The TaskCompletionSource to complete/fault based on the task.</param>
        /// <returns>The new action</returns>
        internal static Action<Task<T>> ResumeWith<T>(TaskCompletionSource<T> tcs)
        {
            return t =>
            {
                if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception);
                }
                else
                {
                    tcs.TrySetResult(t.Result);
                }
            };
        }

        /// <summary>
        /// Create an action that takes a Task and sets the given
        /// TaskCompletionSource to the same state.
        /// </summary>
        /// <param name="tcs">The TaskCompletionSource to complete/fault based on the task.</param>
        /// <returns>The new action</returns>
        internal static Action<Task> ResumeWith(TaskCompletionSource<bool> tcs)
        {
            return t =>
            {
                if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception);
                }
                else
                {
                    tcs.TrySetResult(true); // by convention
                }
            };
        }

        /// <summary>
        /// Terminates the given TaskCompletionSource if the ValueTask completed
        /// or adds a continuation to it which will set the completion state on
        /// The TCS.
        /// </summary>
        /// <param name="task">The task that will be completed.</param>
        /// <param name="tcs">The task completion source to terminate.</param>
        internal static void ResumeWhen(ValueTask task, TaskCompletionSource<bool> tcs)
        {
            if (task.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            if (task.IsFaulted)
            {
                tcs.TrySetException(task.AsTask().Exception);
            }
            else
            if (task.IsCompleted)
            {
                tcs.TrySetResult(true); // by convention
            }
            else
            {
                task.AsTask().ContinueWith(ResumeWith(tcs));
            }
        }

        /// <summary>
        /// Terminates the given TaskCompletionSource (retrieved or created) if the ValueTask completed
        /// or adds a continuation to it which will set the completion state on
        /// The TCS.
        /// </summary>
        /// <param name="task">The task that will be completed.</param>
        /// <param name="target">The target field hosting the resumption task</param>
        internal static void ResumeWhen(ValueTask task, ref TaskCompletionSource<bool> target)
        {
            var tcs = Resume(ref target);
            if (task.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            if (task.IsFaulted)
            {
                tcs.TrySetException(task.AsTask().Exception);
            }
            else
            if (task.IsCompleted)
            {
                tcs.TrySetResult(true); // by convention
            }
            else
            {
                task.AsTask().ContinueWith(ResumeWith(tcs));
            }
        }
    }
}
