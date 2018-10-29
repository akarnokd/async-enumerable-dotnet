// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
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
        /// A singleton instance of a completed task of true.
        /// </summary>
        private static readonly TaskCompletionSource<bool> ResumeTrue;

        static ResumeHelper()
        {
            ResumeTrue = new TaskCompletionSource<bool>();
            ResumeTrue.TrySetResult(true);
        }

        /// <summary>
        /// Atomically indicate resumption by signaling a true completion source
        /// in the target field.
        /// </summary>
        /// <param name="field">The target field to signal through.</param>
        internal static void Resume(ref TaskCompletionSource<bool> field)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref field);
                if (a == null)
                {
                    if (Interlocked.CompareExchange(ref field, ResumeTrue, null) == null)
                    {
                        break;
                    }
                }
                else
                {
                    if (a != ResumeTrue)
                    {
                        a.TrySetResult(true);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Atomically creates a completion source in the target field if
        /// there is none there and returns a task to await it.
        /// </summary>
        /// <param name="field">The target field to use.</param>
        /// <returns>The task to await for completion</returns>
        internal static ValueTask Await(ref TaskCompletionSource<bool> field)
        {
            var b = default(TaskCompletionSource<bool>);
            for (; ; )
            {
                var a = Volatile.Read(ref field);
                if (a == ResumeTrue)
                {
                    return new ValueTask();
                }
                if (a == null)
                {
                    if (b == null)
                    {
                        b = new TaskCompletionSource<bool>();
                    }
                    if (Interlocked.CompareExchange(ref field, b, null) == null)
                    {
                        return new ValueTask(b.Task);
                    }
                }
                else
                {
                    return new ValueTask(a.Task);
                }
            }
        }

        /// <summary>
        /// Clears the target field, preparing it for the next notification round.
        /// </summary>
        /// <param name="field">The field to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Clear(ref TaskCompletionSource<bool> field)
        {
            Interlocked.Exchange(ref field, null);
        }

        /// <summary>
        /// Atomically get or create a task completion source in the target field
        /// and return it.
        /// </summary>
        /// <param name="field">The target field.</param>
        /// <returns>The task completion source retrieved or created.</returns>
        private static TaskCompletionSource<bool> GetOrCreate(ref TaskCompletionSource<bool> field)
        {
            var b = default(TaskCompletionSource<bool>);
            for (; ; )
            {
                var a = Volatile.Read(ref field);
                if (a != null)
                {
                    return a;
                }
                if (b == null)
                {
                    b = new TaskCompletionSource<bool>();
                }
                if (Interlocked.CompareExchange(ref field, b, null) == null)
                {
                    return b;
                }
            }
        }

        /// <summary>
        /// Complete the source in the field based on the ValueTask's outcome.
        /// </summary>
        /// <param name="field">The target field.</param>
        /// <param name="task">The value task</param>
        internal static void Complete(ref TaskCompletionSource<bool> field, ValueTask task)
        {
            var tcs = GetOrCreate(ref field);

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
                tcs.TrySetResult(true);
            }
            else
            {
                task.AsTask()
                    .ContinueWith(Completer, 
                        tcs, 
                        TaskContinuationOptions.ExecuteSynchronously
                    );
            }
        }

        /// <summary>
        /// Lambda that completes a TaskCompletionSource provided as state
        /// </summary>
        private static readonly Action<Task, object> Completer = (task, state) =>
        {
            var tcs = (TaskCompletionSource<bool>)state;
            if (task.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            if (task.IsFaulted)
            {
                tcs.TrySetException(task.Exception);
            }
            else
            {
                tcs.TrySetResult(true);
            }
        };
    }
}
