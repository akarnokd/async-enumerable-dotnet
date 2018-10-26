﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class ToObservable<T> : IObservable<T>
    {
        readonly IAsyncEnumerable<T> _source;

        public ToObservable(IAsyncEnumerable<T> source)
        {
            this._source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var en = _source.GetAsyncEnumerator();
            var handler = new ToObservableHandler(observer, en);
            handler.MoveNext();
            return handler;
        }

        sealed class ToObservableHandler : IDisposable
        {
            readonly IObserver<T> _downstream;

            readonly IAsyncEnumerator<T> _source;

            readonly Action<Task<bool>> _handleMain;
            
            int _wip;

            int _dispose;

            public ToObservableHandler(IObserver<T> downstream, IAsyncEnumerator<T> source)
            {
                this._downstream = downstream;
                this._source = source;
                this._handleMain = t => HandleMain(t);
            }

            internal void MoveNext()
            {
                if (Interlocked.Increment(ref _wip) == 1)
                {
                    do
                    {
                        if (Interlocked.Increment(ref _dispose) == 1)
                        {
                            _source.MoveNextAsync()
                                .AsTask()
                                .ContinueWith(_handleMain);
                        }
                        else
                        {
                            break;
                        }
                    } while (Interlocked.Decrement(ref _wip) != 0);
                }
            }

            void HandleMain(Task<bool> task)
            {
                if (Interlocked.Decrement(ref _dispose) != 0)
                {
                    _source.DisposeAsync();
                } else
                if (task.IsFaulted)
                {
                    _downstream.OnError(ExceptionHelper.Unaggregate(task.Exception));
                }
                else
                {
                    if (task.Result)
                    {
                        _downstream.OnNext(_source.Current);
                        MoveNext();
                    }
                    else
                    {
                        _downstream.OnCompleted();
                    }
                }

            }

            public void Dispose()
            {
                if (Interlocked.Increment(ref _dispose) == 1)
                {
                    _source.DisposeAsync();
                }
            }
        }
    }
}
