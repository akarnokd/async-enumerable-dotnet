// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace async_enumerable_dotnet.impl
{
    /// <summary>
    /// A minimal awaitable construct that supports only one awaiter
    /// and does not convey any value or exception.
    /// </summary>
    internal sealed class SlimResume : INotifyCompletion
    {
        private Action _continuation;

        private static readonly Action CompletedAction = () => { };

        /// <summary>
        /// The singleton instance of a completed SlimResume.
        /// </summary>
        internal static readonly SlimResume Completed;

        static SlimResume()
        {
            Completed = new SlimResume();
            Volatile.Write(ref Completed._continuation, CompletedAction);
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public SlimResume GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted => Volatile.Read(ref _continuation) == CompletedAction;

        public void OnCompleted(Action continuation)
        {
            var old = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (old == CompletedAction)
            {
                continuation.Invoke();
            }
            else
            if (old != null)
            {
                throw new InvalidOperationException("Only one continuation allowed");
            }
        }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public void GetResult()
        {
            // no actual outcome, only resumption
        }

        internal void Signal()
        {
            var prev = Interlocked.Exchange(ref _continuation, CompletedAction);
            prev?.Invoke();
        }
    }
}
