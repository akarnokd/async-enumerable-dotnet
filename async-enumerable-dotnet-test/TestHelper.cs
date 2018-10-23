using async_enumerable_dotnet;
using System;
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

        public static async ValueTask AssertResult<T>(this IAsyncEnumerator<T> source, params T[] values)
        {
            var idx = 0;
            try
            {
                while (await source.MoveNextAsync())
                {
                    Assert.True(idx < values.Length, "Source has more than the expected " + values.Length + " items");
                    Assert.Equal(values[idx], source.Current);
                    idx++;
                }

                Assert.Equal(values.Length, idx);
            }
            finally
            {
                await source.DisposeAsync();
            }
        }

        public static async ValueTask AssertResultSet<T>(this IAsyncEnumerable<T> source, params T[] values)
        {
            var set = new HashSet<T>(values);
            var en = source.GetAsyncEnumerator();
            try
            {
                while (await en.MoveNextAsync())
                {
                    Assert.True(set.Remove(en.Current));
                }

                Assert.Empty(set);
            }
            finally
            {
                await en.DisposeAsync();
            }
        }

        public static ValueTask AssertFailure<T>(this IAsyncEnumerable<T> source, Type exception, params T[] values)
        {
            return AssertFailure(source.GetAsyncEnumerator(), exception, values);
        }

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
                Assert.Equal(values.Length, idx);

                Assert.True(exception.GetTypeInfo().IsAssignableFrom(ex.GetType().GetTypeInfo()), "Wrong exception, Expected: " + exception + ", Actual: " + ex);
            }
            finally
            {
                await source.DisposeAsync();
            }
        }
    }
}
