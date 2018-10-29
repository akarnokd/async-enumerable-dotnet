using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Debounce<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly TimeSpan delay;

        readonly bool emitLast;

        public Debounce(IAsyncEnumerable<T> source, TimeSpan delay, bool emitLast)
        {
            this.source = source;
            this.delay = delay;
            this.emitLast = emitLast;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new DebounceEnumerator(source.GetAsyncEnumerator(), delay, emitLast);
            en.MoveNext();
            return en;
        }

        internal sealed class DebounceEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> source;

            readonly TimeSpan delay;

            readonly Action<Task<bool>> mainHandler;

            readonly bool emitLast;

            public T Current { get; private set; }

            int disposeWip;

            TaskCompletionSource<bool> disposeTask;

            int sourceWip;

            TaskCompletionSource<bool> resume;

            volatile bool done;
            Exception error;

            Node latest;

            T emitLastItem;

            long sourceIndex;

            CancellationTokenSource cts;

            public DebounceEnumerator(IAsyncEnumerator<T> source, TimeSpan delay, bool emitLast)
            {
                this.source = source;
                this.delay = delay;
                this.mainHandler = t => HandleMain(t);
                this.emitLast = emitLast;
            }

            public ValueTask DisposeAsync()
            {
                CancellationHelper.Cancel(ref cts);
                if (Interlocked.Increment(ref disposeWip) == 1)
                {
                    if (emitLast)
                    {
                        emitLastItem = default;
                    }
                    return source.DisposeAsync();
                }
                return ResumeHelper.Await(ref disposeTask);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = done;
                    var v = Interlocked.Exchange(ref latest, null);

                    if (d && v == null)
                    {
                        if (error != null)
                        {
                            throw error;
                        }
                        return false;
                    }
                    else if (v != null)
                    {
                        Current = v.value;
                        return true;
                    }

                    await ResumeHelper.Await(ref resume);
                    ResumeHelper.Clear(ref resume);
                }
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref sourceWip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref disposeWip) == 1)
                        {
                            source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(mainHandler);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref sourceWip) != 0);
                }
            }

            bool TryDispose()
            {
                if (Interlocked.Decrement(ref disposeWip) != 0)
                {
                    if (emitLast)
                    {
                        emitLastItem = default;
                    }
                    ResumeHelper.Complete(ref disposeTask, source.DisposeAsync());
                    return false;
                }
                return true;
            }

            void HandleMain(Task<bool> t)
            {
                if (t.IsFaulted)
                {
                    CancellationHelper.Cancel(ref cts);
                    if (emitLast)
                    {
                        var idx = sourceIndex;
                        if (idx != 0)
                        {
                            SetLatest(emitLastItem, idx + 1);
                            emitLastItem = default;
                        }
                    }
                    error = ExceptionHelper.Extract(t.Exception);
                    done = true;
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref resume);
                    }
                }
                else if (t.Result)
                {
                    Volatile.Read(ref cts)?.Cancel();

                    var v = source.Current;
                    if (TryDispose())
                    {
                        if (emitLast)
                        {
                            emitLastItem = v;
                        }
                        var idx = ++sourceIndex;
                        var newCts = new CancellationTokenSource();
                        if (CancellationHelper.Replace(ref cts, newCts))
                        {
                            Task.Delay(delay, newCts.Token)
                                .ContinueWith(tt => TimerHandler(tt, v, idx));
                            MoveNext();
                        }
                    }
                }
                else
                {
                    CancellationHelper.Cancel(ref cts);
                    if (emitLast)
                    {
                        var idx = sourceIndex;
                        if (idx != 0)
                        {
                            SetLatest(emitLastItem, idx + 1);
                            emitLastItem = default;
                        }
                    }
                    done = true;
                    if (TryDispose())
                    {
                        ResumeHelper.Resume(ref resume);
                    }
                }
            }

            void TimerHandler(Task t, T value, long idx)
            {
                if (!t.IsCanceled && SetLatest(value, idx))
                {
                    ResumeHelper.Resume(ref resume);
                }
            }

            bool SetLatest(T value, long idx)
            {
                var b = default(Node);
                for (; ; )
                {
                    var a = Volatile.Read(ref latest);
                    if (a == null || a.index < idx)
                    {
                        if (b == null)
                        {
                            b = new Node(idx, value);
                        }
                        if (Interlocked.CompareExchange(ref latest, b, a) == a)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            internal sealed class Node
            {
                internal readonly long index;
                internal readonly T value;

                public Node(long index, T value)
                {
                    this.index = index;
                    this.value = value;
                }
            }
        }
    }
}
