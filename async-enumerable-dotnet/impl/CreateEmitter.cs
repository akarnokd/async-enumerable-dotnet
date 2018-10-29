// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class CreateEmitter<T> : IAsyncEnumerable<T>
    {
        private readonly Func<IAsyncEmitter<T>, Task> _handler;

        public CreateEmitter(Func<IAsyncEmitter<T>, Task> handler)
        {
            _handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new CreateEmitterEnumerator();
            en.SetTask(_handler(en));
            return en;
        }

        private sealed class CreateEmitterEnumerator : IAsyncEnumerator<T>, IAsyncEmitter<T>
        {
            private Task _task;

            private volatile bool _disposeRequested;

            private volatile bool _taskComplete;

            public bool DisposeAsyncRequested => _disposeRequested;

            public T Current { get; private set; }

            private TaskCompletionSource<bool> _valueReady;

            private TaskCompletionSource<bool> _consumed;

            internal void SetTask(Task task)
            {
                _task = task;
                task.ContinueWith(t =>
                {
                    _taskComplete = true;
                    ResumeHelper.Resume(ref _valueReady);
                });
            }

            public ValueTask DisposeAsync()
            {
                _disposeRequested = true;
                ResumeHelper.Resume(ref _consumed);
                return new ValueTask(_task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_taskComplete)
                {
                    if (_task.IsFaulted)
                    {
                        throw _task.Exception;
                    }
                    return false;
                }
                ResumeHelper.Resume(ref _consumed);

                await ResumeHelper.Await(ref _valueReady);
                ResumeHelper.Clear(ref _valueReady);

                if (!_taskComplete)
                {
                    return true;
                }

                if (_task.IsFaulted)
                {
                    throw _task.Exception;
                }
                return false;
            }

            public async ValueTask Next(T value)
            {
                if (_disposeRequested)
                {
                    return;
                }
                await ResumeHelper.Await(ref _consumed);
                ResumeHelper.Clear(ref _consumed);
                if (_disposeRequested)
                {
                    return;
                }

                Current = value;

                ResumeHelper.Resume(ref _valueReady);
            }
        }
    }
}
