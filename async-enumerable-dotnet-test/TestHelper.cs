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
        public static async ValueTask AssertResult<T>(this IAsyncEnumerable<T> source, params T[] values)
        {
            var en = source.GetAsyncEnumerator();
            var idx = 0;
            try
            {
                while (await en.MoveNextAsync())
                {
                    Assert.True(idx < values.Length, "Source has more than the expected " + values.Length + " items");
                    Assert.Equal(values[idx], en.Current);
                    idx++;
                }

                Assert.Equal(idx, values.Length);
            }
            finally
            {
                await en.DisposeAsync();
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

        public static async ValueTask AssertFailure<T>(this IAsyncEnumerable<T> source, Type exception, params T[] values)
        {
            var en = source.GetAsyncEnumerator();
            var idx = 0;
            try
            {
                while (await en.MoveNextAsync())
                {
                    Assert.True(idx < values.Length, "Source has more than the expected " + values.Length + " items");
                    Assert.Equal(en.Current, values[idx]);
                    idx++;
                }

                Assert.True(false, "Did not throw the exception " + exception);
            }
            catch (Exception ex)
            {
                Assert.Equal(idx, values.Length);

                Assert.True(exception.GetTypeInfo().IsAssignableFrom(ex.GetType().GetTypeInfo()), "Wrong exception, Expected: " + exception + ", Actual: " + ex);
            }
            finally
            {
                await en.DisposeAsync();
            }
        }
    }
}
