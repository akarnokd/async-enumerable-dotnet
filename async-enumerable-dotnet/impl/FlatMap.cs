using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FlatMap<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, IAsyncEnumerable<R>> mapper;

        readonly int maxConcurrency;

        readonly int prefetch;

        public FlatMap(IAsyncEnumerable<T> source, Func<T, IAsyncEnumerable<R>> mapper, int maxConcurrency, int prefetch)
        {
            this.source = source;
            this.mapper = mapper;
            this.maxConcurrency = maxConcurrency;
            this.prefetch = prefetch;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            var en = new FlatMapEnumerator(source.GetAsyncEnumerator(), mapper, maxConcurrency, prefetch);
            en.MoveNext();
            return en;
        }

        internal sealed class FlatMapEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, IAsyncEnumerable<R>> mapper;

            readonly int maxConcurrency;

            readonly int prefetch;

            readonly ConcurrentQueue<Item> queue;

            TaskCompletionSource<bool> resume;

            InnerHandler[] inners;

            volatile bool done;
            Exception errors;

            static readonly InnerHandler[] EMPTY = new InnerHandler[0];
            static readonly InnerHandler[] TERMINATED = new InnerHandler[0];

            public R Current => current;

            R current;

            long queueWip;

            int dispose;

            int outstanding;

            int sourceWip;

            public FlatMapEnumerator(IAsyncEnumerator<T> source, Func<T, IAsyncEnumerable<R>> mapper, int maxConcurrency, int prefetch)
            {
                this.source = source;
                this.mapper = mapper;
                this.maxConcurrency = maxConcurrency;
                this.queue = new ConcurrentQueue<Item>();
                this.prefetch = prefetch;
                Volatile.Write(ref outstanding, maxConcurrency);
                Volatile.Write(ref inners, EMPTY);
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Increment(ref dispose) == 1)
                {
                    source.DisposeAsync();
                }

                var a = Interlocked.Exchange(ref inners, TERMINATED);
                foreach (var handler in a)
                {
                    handler.Dispose();
                }
                return new ValueTask();
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref sourceWip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref dispose) == 1)
                        {
                            source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(t => Handle(t));
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (Interlocked.Decrement(ref sourceWip) != 0);
                }
            }

            void Handle(Task<bool> task)
            {
                if (Interlocked.Decrement(ref dispose) != 0)
                {
                    source.DisposeAsync();
                }
                else if (task.IsFaulted)
                {
                    AddException(task.Exception);
                    done = true;
                    Signal();
                }
                else
                {
                    if (task.Result)
                    {
                        var innerSource = default(IAsyncEnumerator<R>);
                        try
                        {
                            innerSource = mapper(source.Current)
                                .GetAsyncEnumerator();
                        }
                        catch (Exception ex)
                        {
                            source.DisposeAsync();

                            AddException(ex);
                            done = true;
                            Signal();
                            return;
                        }

                        var handler = new InnerHandler(this, innerSource, prefetch);
                        if (Add(handler))
                        {
                            handler.MoveNext();

                            if (Interlocked.Decrement(ref outstanding) != 0)
                            {
                                MoveNext();
                            }
                        }
                    }
                    else
                    {
                        done = true;
                        Signal();
                    }
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    var d = done && Volatile.Read(ref inners).Length == 0;
                    var success = queue.TryDequeue(out var v);

                    if (d && !success)
                    {
                        if (errors != null)
                        {
                            throw errors;
                        }
                        return false;
                    }
                    else if (success)
                    {
                        if (v.hasValue)
                        {
                            current = v.value;
                            v.sender.ConsumedOne();
                            Interlocked.Exchange(ref resume, null);
                            Interlocked.Decrement(ref queueWip);
                            return true;
                        }
                        else
                        {
                            Remove(v.sender);
                            NextSource();
                            Interlocked.Exchange(ref resume, null);
                            Interlocked.Decrement(ref queueWip);
                            continue;
                        }
                    }

                    if (Volatile.Read(ref queueWip) == 0)
                    {
                        await Resume().Task;
                        Interlocked.Exchange(ref resume, null);
                    }
                }
            }

            bool Add(InnerHandler handler)
            {
                for (; ;)
                {
                    var a = Volatile.Read(ref inners);
                    if (a == TERMINATED)
                    {
                        return false;
                    }
                    var n = a.Length;
                    var b = new InnerHandler[n + 1];
                    Array.Copy(a, 0, b, 0, n);
                    b[n] = handler;

                    if (Interlocked.CompareExchange(ref inners, b, a) == a)
                    {
                        return true;
                    }
                }
            }

            void Remove(InnerHandler handler)
            {
                for (; ; )
                {
                    var a = Volatile.Read(ref inners);
                    var n = a.Length;
                    if (n == 0)
                    {
                        break;
                    }
                    var idx = Array.IndexOf(a, handler);
                    if (idx < 0)
                    {
                        break;
                    }
                    var b = default(InnerHandler[]);
                    if (n == 1)
                    {
                        b = EMPTY;
                    }
                    else
                    {
                        b = new InnerHandler[n - 1];
                        Array.Copy(a, 0, b, 0, idx);
                        Array.Copy(a, idx + 1, b, idx, n - idx - 1);
                    }

                    if (Interlocked.CompareExchange(ref inners, b, a) == a)
                    {
                        handler.Dispose();
                        break;
                    }
                }
            }

            void AddException(Exception ex)
            {
                Interlocked.CompareExchange(ref errors, ex, null);
            }

            void NextSource()
            {
                if (Interlocked.Increment(ref outstanding) == 1)
                {
                    MoveNext();
                }
            }

            internal void InnerNext(InnerHandler sender, R item)
            {
                queue.Enqueue(new Item
                {
                    sender = sender,
                    value = item,
                    hasValue = true
                });
                Signal();
            }

            internal void InnerError(InnerHandler sender, Exception ex)
            {
                AddException(ex);
                queue.Enqueue(new Item
                {
                    sender = sender
                });
                Signal();
            }

            internal void InnerComplete(InnerHandler sender)
            {
                queue.Enqueue(new Item
                {
                    sender = sender
                });
                Signal();
            }

            void Signal()
            {
                if (Interlocked.Increment(ref queueWip) == 1)
                {
                    Resume().TrySetResult(true);
                }
            }

            TaskCompletionSource<bool> Resume()
            {
                var b = default(TaskCompletionSource<bool>);
                for (; ; )
                {
                    var a = Volatile.Read(ref resume);
                    if (a == null)
                    {
                        if (b == null)
                        {
                            b = new TaskCompletionSource<bool>();
                        }
                        if (Interlocked.CompareExchange(ref resume, b, a) == a)
                        {
                            return b;
                        }
                    } else
                    {
                        return a;
                    }
                }
            }
        }

        internal sealed class InnerHandler
        {
            readonly FlatMapEnumerator parent;

            readonly IAsyncEnumerator<R> source;

            readonly int prefetch;

            int dispose;

            int wip;

            int outstanding;

            int consumed;

            public InnerHandler(FlatMapEnumerator parent, IAsyncEnumerator<R> source, int prefetch)
            {
                this.parent = parent;
                this.source = source;
                this.prefetch = prefetch;
                Volatile.Write(ref outstanding, prefetch);
            }

            internal void ConsumedOne()
            {
                int c = consumed + 1;
                int limit = prefetch - (prefetch >> 2);
                if (c == limit)
                {
                    consumed = 0;
                    if (Interlocked.Add(ref outstanding, limit) == limit)
                    {
                        MoveNext();
                    }
                }
                else
                {
                    consumed = c;
                }
            }

            internal void Dispose()
            {
                if (Interlocked.Increment(ref dispose) == 1)
                {
                    source.DisposeAsync();
                }
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref dispose) == 1)
                        {
                            source.MoveNextAsync()
                                .AsTask().ContinueWith(t => Handle(t));
                        }
                        else
                        {
                            break;
                        }
                    } while (Interlocked.Decrement(ref wip) != 0);
                }
            }

            void Handle(Task<bool> task)
            {
                if (Interlocked.Decrement(ref dispose) != 0)
                {
                    source.DisposeAsync();
                }
                else if (task.IsFaulted)
                {
                    parent.InnerError(this, task.Exception);
                }
                else
                {
                    if (task.Result)
                    {
                        parent.InnerNext(this, source.Current);
                        if (Interlocked.Decrement(ref outstanding) != 0)
                        {
                            MoveNext();
                        }
                    }
                    else
                    {
                        parent.InnerComplete(this);
                    }
                }
            }
        }

        internal struct Item
        {
            internal InnerHandler sender;
            internal R value;
            internal bool hasValue;
        }
    }
}
