// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using async_enumerable_dotnet;
using System;

namespace async_enumerable_dotnet_benchmark
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once ArrangeTypeModifiers
    class Program
    {
        /// <summary>
        /// Don't worry about this program yet. I'm using it to
        /// diagnose await hangs and internal state that is otherwise
        /// hard (or I don't know how) to debug as an XUnit test.
        /// </summary>
        /// <param name="args"></param>
        // ReSharper disable once UnusedParameter.Local
        // ReSharper disable once ArrangeTypeMemberModifiers
        static void Main(string[] args)
        {
            for (var j = 0; j < 100000; j++)
            {
                if (j % 10 == 0)
                {
                    Console.WriteLine(j);
                }
                var list = AsyncEnumerable.Create<int>(async e =>
                {
                    for (var i = 0; i < 10 && !e.DisposeAsyncRequested; i++)
                    {
                        await e.Next(i);
                    }
                })
                .Last()
                .GetAsyncEnumerator();

                try
                {
                    if (!list.MoveNextAsync().Result)
                    {
                        Console.WriteLine("Empty?");
                    }

                    if (list.Current != 9)
                    {
                        Console.WriteLine(list.Current);
                        Console.ReadLine();
                        break;
                    }
                }
                finally
                {
                    list.DisposeAsync().AsTask().Wait();
                }
            }
        }
    }
}
