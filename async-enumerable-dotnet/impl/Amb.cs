using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class Amb<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T>[] sources;

        public Amb(IAsyncEnumerable<T>[] sources)
        {
            this.sources = sources;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new AmbEnumerator(sources);
        }

        internal sealed class AmbEnumerator : IAsyncEnumerator<T>
        {
            readonly InnerHandler[] sources;

            readonly TaskCompletionSource<bool> winTask;

            InnerHandler winner;

            int disposeWip;

            TaskCompletionSource<bool> disposeTask;

            Exception disposeError;

            bool once;

            public T Current => winner.source.Current;

            public AmbEnumerator(IAsyncEnumerable<T>[] sources)
            {
                var handlers = new InnerHandler[sources.Length];
                for (var i = 0; i < sources.Length; i++)
                {
                    handlers[i] = new InnerHandler(sources[i].GetAsyncEnumerator(), this);
                }
                this.sources = handlers;
                disposeWip = sources.Length;
                this.disposeTask = new TaskCompletionSource<bool>();
                this.winTask = new TaskCompletionSource<bool>();
            }

            public async ValueTask DisposeAsync()
            {
                winner = null;
                foreach (var ih in sources)
                {
                    ih.Dispose();
                }

                await disposeTask.Task;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (once)
                {
                    return await winner.source.MoveNextAsync();
                }
                else
                {
                    once = true;

                    foreach (var h in sources)
                    {
                        h.MoveNext(t => First(t, h));
                    }

                    var winnerHasValue = await winTask.Task;

                    foreach (var h in sources)
                    {
                        if (h != winner)
                        {
                            h.Dispose();
                        }
                    }

                    return winnerHasValue;
                }
            }

            internal void First(Task<bool> task, InnerHandler sender)
            {
                if (sender.CheckDisposed())
                {
                    Dispose(sender.source);
                }
                else
                {
                    if (Volatile.Read(ref winner) == null && Interlocked.CompareExchange(ref winner, sender, null) == null)
                    {
                        if (task.IsFaulted)
                        {
                            winTask.TrySetException(task.Exception);
                        }
                        else
                        {
                            winTask.SetResult(task.Result);
                        }
                    }
                }
            }

            internal void Dispose(IAsyncDisposable d)
            {
                d.DisposeAsync()
                    .AsTask()
                    .ContinueWith(t => DisposeHandle(t.Exception));
            }

            void DisposeHandle(Exception ex)
            {
                if (ex != null)
                {
                    ExceptionHelper.AddException(ref disposeError, ex);
                }

                if (Interlocked.Decrement(ref disposeWip) == 0)
                {
                    ex = disposeError;
                    disposeError = null;
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

        internal sealed class InnerHandler
        {
            internal readonly IAsyncEnumerator<T> source;

            readonly AmbEnumerator parent;

            int disposeWip;

            public InnerHandler(IAsyncEnumerator<T> source, AmbEnumerator parent)
            {
                this.source = source;
                this.parent = parent;
            }

            internal void Dispose()
            {
                if (Interlocked.Increment(ref disposeWip) == 1)
                {
                    parent.Dispose(source);
                }
            }

            internal void MoveNext(Action<Task<bool>> handler)
            {
                if (Interlocked.Increment(ref disposeWip) == 1)
                {
                    source.MoveNextAsync()
                        .AsTask()
                        .ContinueWith(handler);
                }
            }

            internal bool CheckDisposed()
            {
                return Interlocked.Decrement(ref disposeWip) != 0;
            }
        }
    }
}
