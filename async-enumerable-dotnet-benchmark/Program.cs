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
            for (var i = 0; i < 100000; i++)
            {
                if (i % 100 == 0)
                {
                    Console.WriteLine(i);
                }
                var list = AsyncEnumerable.Range(1, 5)
                    .Publish(a => 
                        a.Take(3).MergeWith(a.Skip(3))
                    )
                    .ToList().GetAsyncEnumerator();
                try
                {
                    /*
                    var t = 0;
                    while (!list.IsCompleted && !list.IsFaulted && !list.IsCanceled && t < 5000)
                    {
                        t++;
                    }

                    while (!list.IsCompleted && !list.IsFaulted && !list.IsCanceled)
                    {
                        await Task.Delay(1);
                    }
                    */

                    if (!list.MoveNextAsync().Result)
                    {
                        Console.WriteLine("Empty?");
                    }

                    if (list.Current.Count != 5)
                    {
                        foreach (var e in list.Current)
                        {
                            Console.Write(e);
                            Console.Write(", ");
                        }
                        Console.WriteLine();
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
