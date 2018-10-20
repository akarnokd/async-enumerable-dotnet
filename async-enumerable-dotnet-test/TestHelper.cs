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
                if (await en.MoveNextAsync())
                {
                    Assert.True(idx < values.Length);
                    Assert.Equal(en.Current, values[idx]);
                    idx++;
                }
                else
                {
                    Assert.Equal(idx, values.Length);
                }
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
                if (await en.MoveNextAsync())
                {
                    Assert.True(idx < values.Length);
                    Assert.Equal(en.Current, values[idx]);
                    idx++;
                }
                else
                {
                    Assert.True(false, "Did not throw the exception " + exception);
                }
            }
            catch (Exception ex)
            {
                Assert.Equal(idx, values.Length);

                Assert.True(exception.GetTypeInfo().IsAssignableFrom(ex.GetType().GetTypeInfo()));
            }
            finally
            {
                await en.DisposeAsync();
            }
        }
    }
}
