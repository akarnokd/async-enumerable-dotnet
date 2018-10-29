// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using async_enumerable_dotnet;
using System;
using System.Threading.Tasks;

namespace async_enumerable_dotnet_benchmark
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once ArrangeTypeModifiers
    class Program
    {
        // ReSharper disable once UnusedParameter.Local
        // ReSharper disable once ArrangeTypeMemberModifiers
        static async Task Main(string[] args)
        {
            var result = AsyncEnumerable.Range(1, 5)
                .Map(v =>
                {
                    Console.WriteLine(v);
                    return v;
                })
                .FlatMap(v => AsyncEnumerable.Range(v * 10, 5)
                .Map(w =>
                {
                    Console.WriteLine(w);
                    return w;
                })
                , 1, 1)
                ;

            var en = result.GetAsyncEnumerator();
            try
            {
                while (await en.MoveNextAsync())
                {
                    Console.WriteLine(">> " + en.Current);
                }
            }
            finally
            {
                await en.DisposeAsync();
            }
        }
    }
}
