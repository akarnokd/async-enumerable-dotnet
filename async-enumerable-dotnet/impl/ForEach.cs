using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal static class ForEach
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async ValueTask Consume<T>(this IAsyncEnumerable<T> source, IAsyncConsumer<T> consumer, CancellationToken ct)
        {
            var en = source.GetAsyncEnumerator();
            try
            {
                if (!ct.IsCancellationRequested)
                {
                    try
                    {
                        while (await en.MoveNextAsync())
                        {
                            if (ct.IsCancellationRequested)
                            {
                                return;
                            }
                            await consumer.Next(en.Current);
                        }

                        await consumer.Complete();
                    }
                    catch (Exception ex)
                    {
                        await consumer.Error(ex);
                    }
                }
            }
            finally
            {
                await en.DisposeAsync();
            }
        }

    }
}
