using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class GroupBy<T, K, V> : IAsyncEnumerable<IAsyncGroupedEnumerable<K, V>>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, K> keySelector;

        readonly Func<T, V> valueSelector;

        readonly IEqualityComparer<K> keyComparer;

        public GroupBy(IAsyncEnumerable<T> source, Func<T, K> keySelector, Func<T, V> valueSelector, IEqualityComparer<K> keyComparer)
        {
            this.source = source;
            this.keySelector = keySelector;
            this.valueSelector = valueSelector;
            this.keyComparer = keyComparer;
        }

        public IAsyncEnumerator<IAsyncGroupedEnumerable<K, V>> GetAsyncEnumerator()
        {
            return new GroupByEnumerator(source.GetAsyncEnumerator(), keySelector, valueSelector, keyComparer);
        }

        internal sealed class GroupByEnumerator : IAsyncEnumerator<IAsyncGroupedEnumerable<K, V>>
        {
            readonly IAsyncEnumerator<T> source;

            readonly Func<T, K> keySelector;

            readonly Func<T, V> valueSelector;

            readonly ConcurrentDictionary<K, Group> groups;

            int active;

            public IAsyncGroupedEnumerable<K, V> Current { get; private set; }

            bool done;

            public GroupByEnumerator(IAsyncEnumerator<T> source, Func<T, K> keySelector, Func<T, V> valueSelector, IEqualityComparer<K> keyComparer)
            {
                this.source = source;
                this.keySelector = keySelector;
                this.valueSelector = valueSelector;
                this.groups = new ConcurrentDictionary<K, Group>(keyComparer);
                Volatile.Write(ref active, 1);
            }

            public async ValueTask DisposeAsync()
            {
                if (!done)
                {
                    for (; ; )
                    {
                        try
                        {
                            if (groups.IsEmpty)
                            {
                                break;
                            }
                            if (await source.MoveNextAsync())
                            {
                                var t = source.Current;
                                var k = keySelector(t);

                                var found = groups.TryGetValue(k, out var g);

                                if (found)
                                {
                                    await g.Next(valueSelector(t));
                                }
                            }
                            else
                            {
                                foreach (var gr in groups)
                                {
                                    await gr.Value.Complete();
                                }
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            foreach (var gr in groups)
                            {
                                await gr.Value.Error(ex);
                            }
                            break;
                        }
                    }
                }
                if (Interlocked.Decrement(ref active) == 0)
                {
                    await source.DisposeAsync();
                }
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ;)
                {
                    try
                    {
                        if (await source.MoveNextAsync())
                        {
                            var t = source.Current;
                            var k = keySelector(t);

                            var found = groups.TryGetValue(k, out var g);

                            if (!found)
                            {
                                g = new Group(k, this);
                                Interlocked.Increment(ref active);
                                groups.TryAdd(k, g);
                                Current = g;
                            }

                            await g.Next(valueSelector(t));

                            if (!found)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            foreach (var gr in groups)
                            {
                                await gr.Value.Complete();
                            }
                            done = true;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        foreach (var gr in groups)
                        {
                            await gr.Value.Error(ex);
                        }
                        done = true;
                        return false;
                    }
                }
            }

            internal async ValueTask Remove(Group g)
            {
                groups.TryRemove(g.Key, out _);
                if (Interlocked.Decrement(ref active) == 0)
                {
                    await source.DisposeAsync();
                }
            }

            internal sealed class Group : IAsyncGroupedEnumerable<K, V>, IAsyncEnumerator<V>, IAsyncConsumer<V>
            {
                readonly GroupByEnumerator parent;

                public K Key { get; }

                public V Current { get; private set; }

                int once;

                V current;
                Exception error;
                bool done;

                TaskCompletionSource<bool> consumed;

                TaskCompletionSource<bool> valueReady;

                public Group(K key, GroupByEnumerator parent)
                {
                    Key = key;
                    this.parent = parent;
                    ResumeHelper.Resume(ref consumed).TrySetResult(true);
                }

                public async ValueTask Next(V value)
                {
                    await ResumeHelper.Resume(ref consumed).Task;
                    ResumeHelper.Clear(ref consumed);

                    current = value;

                    ResumeHelper.Resume(ref valueReady).TrySetResult(true);
                }

                public async ValueTask Error(Exception ex)
                {
                    await ResumeHelper.Resume(ref consumed).Task;
                    ResumeHelper.Clear(ref consumed);

                    error = ex;
                    done = true;

                    ResumeHelper.Resume(ref valueReady).TrySetResult(true);
                }

                public async ValueTask Complete()
                {
                    await ResumeHelper.Resume(ref consumed).Task;
                    ResumeHelper.Clear(ref consumed);

                    done = true;

                    ResumeHelper.Resume(ref valueReady).TrySetResult(true);
                }


                public IAsyncEnumerator<V> GetAsyncEnumerator()
                {
                    if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                    {
                        return this;
                    }
                    return new Error<V>.ErrorEnumerator(new InvalidOperationException("Only one IAsyncEnumerator supported"));
                }

                public async ValueTask<bool> MoveNextAsync()
                {
                    await ResumeHelper.Resume(ref valueReady).Task;
                    ResumeHelper.Clear(ref valueReady);

                    if (done)
                    {
                        if (error != null)
                        {
                            throw error;
                        }
                        return false;
                    }
                    Current = current;
                    ResumeHelper.Resume(ref consumed).TrySetResult(true);
                    return true;
                }

                public async ValueTask DisposeAsync()
                {
                    await parent.Remove(this);
                    ResumeHelper.Resume(ref consumed).TrySetResult(true);
                }
            }
        }
    }
}
