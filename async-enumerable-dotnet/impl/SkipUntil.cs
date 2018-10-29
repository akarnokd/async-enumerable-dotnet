using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class SkipUntil<T, U> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly IAsyncEnumerable<U> other;

        public SkipUntil(IAsyncEnumerable<T> source, IAsyncEnumerable<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new SkipUntilEnumerator(source.GetAsyncEnumerator(), other.GetAsyncEnumerator());
            en.MoveNextOther();
            en.MoveNextMain();
            return en;
        }

        internal sealed class SkipUntilEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly IAsyncEnumerator<U> other;

            int disposeMain;

            int disposeOther;

            int disposed;

            Exception disposeErrors;

            TaskCompletionSource<bool> disposeTask;

            Exception error;
            bool done;
            bool hasValue;
            T current;

            TaskCompletionSource<bool> resume;

            int gate;

            int wipMain;

            public SkipUntilEnumerator(IAsyncEnumerator<T> source, IAsyncEnumerator<U> other)
            {
                this.source = source;
                this.other = other;
                this.disposeTask = new TaskCompletionSource<bool>();
                Volatile.Write(ref disposed, 2);
            }

            public T Current => current;

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref disposeMain) == 1)
                {
                    Dispose(source);
                }

                if (Interlocked.Increment(ref disposeOther) == 1)
                {
                    Dispose(other);
                }

                await disposeTask.Task;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    var ex = Volatile.Read(ref error);
                    if (ex != null)
                    {
                        throw ex;
                    }

                    var d = Volatile.Read(ref done);
                    var e = Volatile.Read(ref hasValue);

                    if (d && !e)
                    {
                        return false;
                    }

                    if (e)
                    {
                        hasValue = false;
                        var next = false;
                        if (Volatile.Read(ref gate) != 0)
                        {
                            next = true;
                            current = source.Current;
                        }
                        MoveNextMain();
                        if (next)
                        {
                            return true;
                        }
                    }

                    await ResumeHelper.Await(ref resume);
                    ResumeHelper.Clear(ref resume);
                }
            }

            internal void MoveNextMain()
            {
                if (Interlocked.Increment(ref wipMain) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref disposeMain) == 1)
                        {
                            source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(t => HandleMain(t));
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref wipMain) != 0);
                }
            }

            void HandleMain(Task<bool> t)
            {
                if (Interlocked.Decrement(ref disposeMain) != 0)
                {
                    Dispose(source);
                }
                else if (t.IsFaulted)
                {
                    Interlocked.CompareExchange(ref error, t.Exception, null);
                    Signal();
                }
                else
                {
                    if (t.Result)
                    {
                        Volatile.Write(ref hasValue, true);
                    }
                    else
                    {
                        Volatile.Write(ref done, true);
                    }
                    Signal();
                }
            }

            internal void MoveNextOther()
            {
                if (Interlocked.Increment(ref disposeOther) == 1){
                    other.MoveNextAsync().AsTask()
                        .ContinueWith(t => HandleOther(t));
                }
            }

            public void HandleOther(Task<bool> t)
            {
                if (Interlocked.Decrement(ref disposeOther) != 0)
                {
                    Dispose(other);
                }
                else
                {
                    if (t.IsFaulted)
                    {
                        Interlocked.CompareExchange(ref error, t.Exception, null);
                        Signal();
                    }
                    else
                    {
                        Interlocked.Exchange(ref gate, 1);
                        Signal();
                    }

                    if (Interlocked.Increment(ref disposeOther) == 1)
                    {
                        Dispose(other);
                    }
                }
            }

            void Signal()
            {
                ResumeHelper.Resume(ref resume);
            }

            void Dispose(IAsyncDisposable d)
            {
                d.DisposeAsync()
                    .AsTask().ContinueWith(t =>
                    {
                        DisposeHandler(t);
                    });
            }

            void DisposeHandler(Task t)
            {
                if (t.IsFaulted)
                {
                    ExceptionHelper.AddException(ref disposeErrors, t.Exception);
                }
                if (Interlocked.Decrement(ref disposed) == 0)
                {
                    var ex = disposeErrors;
                    if (ex != null)
                    {
                        disposeTask.TrySetException(ex);
                    }
                    else
                    {
                        disposeTask.TrySetResult(false);
                    }
                }
            }
        }
    }
}
