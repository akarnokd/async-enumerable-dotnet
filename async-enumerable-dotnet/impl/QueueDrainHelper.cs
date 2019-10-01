// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace async_enumerable_dotnet.impl
{
    /// <summary>
    /// Utility methods for common queue-drain operations.
    /// </summary>
    internal static class QueueDrainHelper
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MoveNext<TSource>(IAsyncEnumerator<TSource> source, ref int sourceWip, ref int disposeWip, Action<Task<bool>, object> nextHandlerAction, object sender)
        {
            if (Interlocked.Increment(ref sourceWip) == 1)
            {
                do
                {
                    if (Interlocked.Increment(ref disposeWip) == 1)
                    {
                        source.MoveNextAsync()
                            .AsTask()
                            .ContinueWith(nextHandlerAction, sender);
                    }
                    else
                    {
                        break;
                    }
                }
                while (Interlocked.Decrement(ref sourceWip) != 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisposeHandler(Task t, ref int allDisposeWip, ref Exception allDisposeError, TaskCompletionSource<bool> allDisposeTask)
        {
            if (t.IsFaulted)
            {
                ExceptionHelper.AddException(ref allDisposeError, ExceptionHelper.Extract(t.Exception));
            }
            DisposeOne(ref allDisposeWip, ref allDisposeError, allDisposeTask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisposeOne(ref int allDisposeWip, ref Exception allDisposeError, TaskCompletionSource<bool> allDisposeTask)
        {
            if (Interlocked.Decrement(ref allDisposeWip) == 0)
            {
                var ex = allDisposeError;
                if (ex != null)
                {
                    allDisposeError = null;
                    allDisposeTask.TrySetException(ex);
                }
                else
                {
                    allDisposeTask.TrySetResult(true);
                }
            }
        }
    }
}
