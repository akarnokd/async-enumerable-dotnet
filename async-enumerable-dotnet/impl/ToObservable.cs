using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ToObservable<T> : IObservable<T>
    {
        readonly IAsyncEnumerable<T> source;

        public ToObservable(IAsyncEnumerable<T> source)
        {
            this.source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var en = source.GetAsyncEnumerator();
            var handler = new ToObservableHandler(observer, en);
            handler.MoveNext();
            return handler;
        }

        internal sealed class ToObservableHandler : IDisposable
        {
            readonly IObserver<T> downstream;

            readonly IAsyncEnumerator<T> source;

            int wip;

            int dispose;

            public ToObservableHandler(IObserver<T> downstream, IAsyncEnumerator<T> source)
            {
                this.downstream = downstream;
                this.source = source;
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref dispose) == 1)
                        {
                            var vt = source.MoveNextAsync();

                            if (vt.IsFaulted || vt.IsCompleted)
                            {
                                Handle(vt);
                            }
                            else
                            {
                                vt.AsTask().ContinueWith(t => Handle(t));
                            }
                        }
                        else
                        {
                            break;
                        }
                    } while (Interlocked.Decrement(ref wip) != 0);
                }
            }

            void Handle(ValueTask<bool> vtask)
            {
                if (vtask.IsFaulted)
                {
                    try
                    {
                        var v = vtask.Result;
                    }
                    catch (Exception ex)
                    {
                        downstream.OnError(ex);
                    }
                }
                else
                {
                    if (vtask.Result)
                    {
                        downstream.OnNext(source.Current);
                        MoveNext();
                    }
                    else
                    {
                        downstream.OnCompleted();
                    }
                }

                if (Interlocked.Decrement(ref dispose) != 0)
                {
                    source.DisposeAsync();
                }
            }

            void Handle(Task<bool> task)
            {
                if (task.IsFaulted)
                {
                    downstream.OnError(task.Exception);
                }
                else
                {
                    if (task.Result)
                    {
                        downstream.OnNext(source.Current);
                        MoveNext();
                    }
                    else
                    {
                        downstream.OnCompleted();
                    }
                }

                if (Interlocked.Decrement(ref dispose) != 0)
                {
                    source.DisposeAsync();
                }
            }

            public void Dispose()
            {
                if (Interlocked.Increment(ref dispose) == 1)
                {
                    source.DisposeAsync();
                }
            }
        }
    }
}
