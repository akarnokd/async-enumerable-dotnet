// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet
{
    /// <summary>
    /// Allows creating immediate and timed tasks that fire when a
    /// virtual time is moved forward.
    /// </summary>
    public sealed class TestTaskRunner
    {
        /// <summary>
        /// The current virtual time, use Volatile to access it!
        /// </summary>
        private long _now;

        /// <summary>
        /// The sorted list of tasks based on their due time.
        /// </summary>
        private readonly SortedList<IndexDueTime, TaskTask> _queue;

        /// <summary>
        /// The current virtual time.
        /// </summary>
        public long Now => Volatile.Read(ref _now);

        /// <summary>
        /// Atomically incremented index to break ties between tasks
        /// </summary>
        private long _index;

        /// <summary>
        /// True if there are still outstanding tasks.
        /// </summary>
        public bool HasTasks
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count != 0;
                }
            }
        }

        /// <summary>
        /// Construct a fresh TestTaskRunner with the given
        /// (optional) start virtual time.
        /// </summary>
        /// <param name="startTime">The virtual time to start with.</param>
        public TestTaskRunner(long startTime = 0L)
        {
            _now = startTime;
            _queue = new SortedList<IndexDueTime, TaskTask>(IndexDueTimeComparer.Default);
        }

        /// <summary>
        /// Advance the virtual time by the given amount and
        /// signal all timed tasks in the meantime.
        /// </summary>
        /// <param name="milliseconds">The time to move forward</param>
        public void AdvanceTimeBy(long milliseconds)
        {
            var now = Volatile.Read(ref _now) + milliseconds;

            for (; ; )
            {
                var t = default(TaskTask);
                var has = false;
                lock (_queue)
                {
                    if (_queue.Count != 0)
                    {
                        t = _queue.Values[0];
                        if (t.DueTime <= now)
                        {
                            has = true;
                            _queue.RemoveAt(0);
                        }
                    }
                }

                if (has)
                {
                    Volatile.Write(ref _now, t.DueTime);
                    t.Signal();
                }
                else
                {
                    break;
                }
            }
            Volatile.Write(ref _now, now);
        }

        private void Enqueue(TaskTask task)
        {
            lock (_queue)
            {
                _queue.Add(new IndexDueTime
                {
                    Index = _index++,
                    DueTime = task.DueTime
                }, task);
            }
        }

        /// <summary>
        /// Creates a task to be awaited and is fired
        /// after the specified virtual time elapses
        /// when it signals an exception.
        /// </summary>
        /// <param name="error">The error to signal</param>
        /// <param name="delayMillis">The time to delay this task.</param>
        /// <returns>The task to await</returns>
        public Task CreateErrorTask(Exception error, long delayMillis = 0L)
        {
            var now = Volatile.Read(ref _now) + delayMillis;
            var tcs = new TaskCompletionSource<bool>();
            Enqueue(new ErrorTaskTask<bool>(now, tcs, error));
            return tcs.Task;
        }

        /// <summary>
        /// Creates a task to be awaited and is fired
        /// after the specified virtual time elapses
        /// when it signals an exception.
        /// </summary>
        /// <param name="error">The error to signal</param>
        /// <param name="delayMillis">The time to delay this task.</param>
        /// <returns>The task to await</returns>
        public Task<T> CreateErrorTask<T>(Exception error, long delayMillis = 0L)
        {
            var now = Volatile.Read(ref _now) + delayMillis;
            var tcs = new TaskCompletionSource<T>();
            Enqueue(new ErrorTaskTask<T>(now, tcs, error));
            return tcs.Task;
        }

        /// <summary>
        /// Creates a task to be awaited and is fired
        /// after the specified virtual time elapses
        /// </summary>
        /// <param name="delayMillis">The time to delay this task.</param>
        /// <returns>The task to await</returns>
        public Task CreateCompleteTask(long delayMillis = 0L)
        {
            var now = Volatile.Read(ref _now) + delayMillis;
            var tcs = new TaskCompletionSource<bool>();
            Enqueue(new ValueTaskTask<bool>(now, tcs, true));
            return tcs.Task;
        }

        /// <summary>
        /// Creates a task to be awaited and is fired
        /// after the specified virtual time elapses, resulting
        /// in the given success value.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="item">The item to signal as the result.</param>
        /// <param name="delayMillis">The time to delay this task.</param>
        /// <returns>The task to await</returns>
        public Task<T> CreateValueTask<T>(T item, long delayMillis = 0L)
        {
            var now = Volatile.Read(ref _now) + delayMillis;
            var tcs = new TaskCompletionSource<T>();
            Enqueue(new ValueTaskTask<T>(now, tcs, item));
            return tcs.Task;
        }

        /// <summary>
        /// Creates a task to be awaited and is canceled after
        /// the specified virtual time elapses.
        /// </summary>
        /// <param name="delayMillis">The time to delay this task.</param>
        /// <returns>The task to await</returns>
        public Task CreateCancelTask(long delayMillis = 0L)
        {
            var now = Volatile.Read(ref _now) + delayMillis;
            var tcs = new TaskCompletionSource<bool>();
            Enqueue(new CanceledTaskTask<bool>(now, tcs));
            return tcs.Task;
        }
        
        /// <summary>
        /// Creates a task to be awaited and is canceled after
        /// the specified virtual time elapses.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="delayMillis">The time to delay this task.</param>
        /// <returns>The task to await</returns>
        public Task<T> CreateCancelTask<T>(long delayMillis = 0L)
        {
            var now = Volatile.Read(ref _now) + delayMillis;
            var tcs = new TaskCompletionSource<T>();
            Enqueue(new CanceledTaskTask<T>(now, tcs));
            return tcs.Task;
        }
        
        /// <summary>
        /// Creates a task to be awaited and calls the specified
        /// action before completing when
        /// the specified virtual time elapses.
        /// </summary>
        /// <param name="action">The action to invoke before completing the task.</param>
        /// <param name="delayMillis">The time to delay this task.</param>
        /// <returns>The task to await</returns>
        public Task CreateActionTask(Action action, long delayMillis = 0L)
        {
            var now = Volatile.Read(ref _now) + delayMillis;
            var tcs = new TaskCompletionSource<bool>();
            Enqueue(new ActionTaskTask(now, tcs, action));
            return tcs.Task;
        }

        /// <summary>
        /// Creates a task to be awaited and calls the specified
        /// action with the completion token when
        /// the specified virtual time elapses.
        /// The action should then signal the appropriate completion mode.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        /// <param name="delayMillis">The time to delay this task.</param>
        /// <returns>The task to await</returns>
        public Task<T> CreateLambdaTask<T>(Action<TaskCompletionSource<T>> action, long delayMillis = 0L)
        {
            var now = Volatile.Read(ref _now) + delayMillis;
            var tcs = new TaskCompletionSource<T>();
            Enqueue(new LambdaTaskTask<T>(now, tcs, action));
            return tcs.Task;
        }

        /// <summary>
        /// The base class for remembering a due time and signaling.
        /// </summary>
        private abstract class TaskTask
        {
            /// <summary>
            /// When this task should run.
            /// </summary>
            internal readonly long DueTime;

            /// <summary>
            /// Implement this to perform the action when the DueTime
            /// has come.
            /// </summary>
            internal abstract void Signal();

            /// <summary>
            /// Constructs a TaskTask with the given due time.
            /// </summary>
            /// <param name="dueTime">When to fire this task.</param>
            protected TaskTask(long dueTime)
            {
                DueTime = dueTime;
            }
        }

        /// <summary>
        /// A task that signals the given value after a delay
        /// </summary>
        /// <typeparam name="T">The element type to signal.</typeparam>
        private sealed class ValueTaskTask<T> : TaskTask
        {
            private readonly TaskCompletionSource<T> _tcs;

            private readonly T _value;

            public ValueTaskTask(long dueTime, TaskCompletionSource<T> tcs, T value) : base(dueTime)
            {
                _tcs = tcs;
                _value = value;
            }

            internal override void Signal()
            {
                _tcs.TrySetResult(_value);
            }
        }

        /// <summary>
        /// A task that signals an error after a delay.
        /// </summary>
        /// <typeparam name="T">The element type of the task.</typeparam>
        private sealed class ErrorTaskTask<T> : TaskTask
        {
            private readonly TaskCompletionSource<T> _tcs;

            private readonly Exception _error;

            public ErrorTaskTask(long dueTime, TaskCompletionSource<T> tcs, Exception error) : base(dueTime)
            {
                _tcs = tcs;
                _error = error;
            }

            internal override void Signal()
            {
                _tcs.TrySetException(_error);
            }
        }

        /// <summary>
        /// A task that signals cancellation after a delay.
        /// </summary>
        /// <typeparam name="T">The element type of the task.</typeparam>
        private sealed class CanceledTaskTask<T> : TaskTask
        {
            private readonly TaskCompletionSource<T> _tcs;

            public CanceledTaskTask(long dueTime, TaskCompletionSource<T> tcs) : base(dueTime)
            {
                _tcs = tcs;
            }

            internal override void Signal()
            {
                _tcs.TrySetCanceled();
            }
        }

        /// <summary>
        /// Execute an action upon the due time and succeed the task.
        /// </summary>
        private sealed class ActionTaskTask : TaskTask
        {
            private readonly TaskCompletionSource<bool> _tcs;

            private readonly Action _action;

            public ActionTaskTask(long dueTime, TaskCompletionSource<bool> tcs, Action action) : base(dueTime)
            {
                _tcs = tcs;
                _action = action;
            }


            internal override void Signal()
            {
                try
                {
                    _action();
                    _tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }
        }
        
        /// <summary>
        /// Execute a lambda at due time and have it set the outcome
        /// of the task.
        /// </summary>
        /// <typeparam name="T">The value type of the task.</typeparam>
        private sealed class LambdaTaskTask<T> : TaskTask
        {
            private readonly TaskCompletionSource<T> _tcs;

            private readonly Action<TaskCompletionSource<T>> _action;

            public LambdaTaskTask(long dueTime, TaskCompletionSource<T> tcs, Action<TaskCompletionSource<T>> action) : base(dueTime)
            {
                _tcs = tcs;
                _action = action;
            }


            internal override void Signal()
            {
                try
                {
                    _action(_tcs);
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }
        }

        /// <summary>
        /// A composite for the due time and an unique sequential
        /// index.
        /// </summary>
        private struct IndexDueTime
        {
            internal long Index;
            internal long DueTime;
        }

        /// <summary>
        /// Compares two IndexDueTime instances, DueTime first, Index next.
        /// </summary>
        private sealed class IndexDueTimeComparer : IComparer<IndexDueTime>
        {
            internal static readonly IComparer<IndexDueTime> Default = new IndexDueTimeComparer();

            public int Compare(IndexDueTime x, IndexDueTime y)
            {
                var c = x.DueTime.CompareTo(y.DueTime);
                if (c == 0)
                {
                    c = x.Index.CompareTo(y.Index);
                }
                return c;
            }
        }
    }
}
