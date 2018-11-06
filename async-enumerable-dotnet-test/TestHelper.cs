// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using async_enumerable_dotnet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace async_enumerable_dotnet_test
{
    internal static class TestHelper
    {
        public static ValueTask AssertResult<T>(this IAsyncEnumerable<T> source, params T[] values)
        {
            return AssertResult(source.GetAsyncEnumerator(), values);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static async ValueTask AssertResult<T>(this IAsyncEnumerator<T> source, params T[] values)
        {
            var main = default(Exception);
            var dispose = default(Exception);
            var idx = 0;
            try
            {
                while (await source.MoveNextAsync())
                {
                    Assert.True(idx < values.Length, "Source has more than the expected " + values.Length + " items");
                    Assert.Equal(values[idx], source.Current);
                    idx++;
                }

                Assert.True(values.Length == idx, "Source has less items than expected: " + values.Length + ", actual: " + idx);
            }
            catch (Exception ex)
            {
                main = ex;
            }
            finally
            {
                try
                {
                    await source.DisposeAsync();
                }
                catch (Exception ex)
                {
                    dispose = ex;
                }
            }

            if (main != null && dispose != null)
            {
                throw new AggregateException(main, dispose);
            }
            if (main != null)
            {
                throw main;
            }
            if (dispose != null)
            {
                throw dispose;
            }
        }

        public static async ValueTask AssertThen<T>(this IAsyncEnumerable<T> source, Action<IList<T>> outcomeHandler)
        {
            var list = await source.ToListAsync();
            outcomeHandler(list);
        }

        public static ValueTask AssertResultSet<T>(this IAsyncEnumerable<T> source, params T[] values)
        {
            return AssertResultSet(source, EqualityComparer<T>.Default, values);
        }

        public static async ValueTask AssertResultSet<T>(this IAsyncEnumerable<T> source, IEqualityComparer<T> comparer, params T[] values)
        {
            var set = new HashSet<T>(values, comparer);
            var en = source.GetAsyncEnumerator();
            try
            {
                while (await en.MoveNextAsync())
                {
                    var c = en.Current;
                    Assert.Contains(c, set);
                    Assert.True(set.Remove(c), "Unexpected item: " + AsString(c));
                }

                Assert.Empty(set);
            }
            finally
            {
                await en.DisposeAsync();
            }
        }

        private static string AsString(object obj)
        {
            if (obj is IEnumerable en)
            {
                var sb = new StringBuilder("[");
                var i = 0;
                foreach (var o in en)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(o);
                    i++;
                }
                sb.Append("]");
                return sb.ToString();
            }
            return obj != null ? obj.ToString() : "null";
        }

        public static ValueTask AssertFailure<T>(this IAsyncEnumerable<T> source, Type exception, params T[] values)
        {
            return AssertFailure(source.GetAsyncEnumerator(), exception, values);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static async ValueTask AssertFailure<T>(this IAsyncEnumerator<T> source, Type exception, params T[] values)
        {
            var idx = 0;
            try
            {
                while (await source.MoveNextAsync())
                {
                    Assert.True(idx < values.Length, "Source has more than the expected " + values.Length + " items");
                    Assert.Equal(source.Current, values[idx]);
                    idx++;
                }

                Assert.True(false, "Did not throw the exception " + exception);
            }
            catch (Exception ex)
            {
                Assert.True(values.Length == idx, "Source has less items than expected: " + values.Length + ", actual: " + idx);

                Assert.True(exception.GetTypeInfo().IsAssignableFrom(ex.GetType().GetTypeInfo()), "Wrong exception, Expected: " + exception + ", Actual: " + ex);
            }
            finally
            {
                await source.DisposeAsync();
            }
        }

        /// <summary>
        /// Emits the given numbers after the same milliseconds from
        /// the start.
        /// </summary>
        /// <param name="timestamps">The params array of timestamps.</param>
        /// <returns>The new IAsyncEnumerable sequence.</returns>
        internal static IAsyncEnumerable<long> TimeSequence(params long[] timestamps)
        {
            return AsyncEnumerable.FromArray(timestamps)
                .FlatMap(v => AsyncEnumerable.Timer(TimeSpan.FromMilliseconds(v)).Map(w => v));
        }
    }
}
