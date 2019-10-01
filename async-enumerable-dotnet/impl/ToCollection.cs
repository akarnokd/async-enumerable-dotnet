// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal static class ToCollection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static async ValueTask<List<T>> ToList<T>(IAsyncEnumerable<T> source)
        {
            var result = new List<T>();
            var en = source.GetAsyncEnumerator(default);
            try
            {
                while (await en.MoveNextAsync())
                {
                    result.Add(en.Current);
                }
            }
            finally
            {
                await en.DisposeAsync();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static async ValueTask<T[]> ToArray<T>(IAsyncEnumerable<T> source)
        {
            var en = source.GetAsyncEnumerator(default);
            try
            {
                if (await en.MoveNextAsync())
                {
                    var result = new List<T>();
                    do
                    {
                        result.Add(en.Current);
                    } while (await en.MoveNextAsync());

                    return result.ToArray();
                }
                else
                {
                    return Array.Empty<T>();
                }
            }
            finally
            {
                await en.DisposeAsync();
            }
        }
    }
}
