using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal static class ForEach
    {
        internal static async ValueTask ForEachAction<T>(IAsyncEnumerable<T> source, Action<T> onNext, Action<Exception> onError, Action onComplete)
        {
            var enumerator = source.GetAsyncEnumerator();
            try
            {
                for (; ; )
                {
                    var b = false;
                    try
                    {
                        b = await enumerator.MoveNextAsync();
                        if (b)
                        {
                            onNext?.Invoke(enumerator.Current);
                        }
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke(ex);
                        break;
                    }

                    if (!b)
                    {
                        onComplete?.Invoke();
                        break;
                    }

                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

    }
}
