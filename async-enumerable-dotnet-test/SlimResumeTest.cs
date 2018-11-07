// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using Xunit;
using async_enumerable_dotnet.impl;
using System.Threading.Tasks;
using System.Threading;

namespace async_enumerable_dotnet_test
{
    public class SlimResumeTest
    {
        [Fact]
        public async void ReadyUpfront()
        {
            var rsm = new SlimResume();
            rsm.Signal();

            await rsm;
        }

        [Fact]
        public async void Completed()
        {
            var rsm = SlimResume.Completed;

            await rsm;
        }

        [Fact]
        public async void SignalLater()
        {
            var rsm = new SlimResume();

            var t = Task.Delay(100)
                .ContinueWith(t0 => rsm.Signal());

            await rsm;

            await t;
        }


        [Fact]
        public async void Race()
        {
            for (var i = 0; i < 10_000; i++)
            {
                var rsm = new SlimResume();

                var wip = 2;

                var t1 = Task.Factory.StartNew(() =>
                {
                    if (Interlocked.Decrement(ref wip) != 0)
                    {
                        while (Volatile.Read(ref wip) != 0) { }
                    }

                    rsm.Signal();
                }, TaskCreationOptions.LongRunning);

                var t2 = Task.Factory.StartNew(async () =>
                {
                    if (Interlocked.Decrement(ref wip) != 0)
                    {
                        while (Volatile.Read(ref wip) != 0) { }
                    }

                    await rsm;
                }, TaskCreationOptions.LongRunning);

                await t1;

                await t2;
            }
        }

        [Fact]
        public void OneAwaiterMax()
        {
            var rsm = new SlimResume();

            rsm.OnCompleted(() => { });

            try
            {
                rsm.OnCompleted(() => { });
                Assert.False(true, "Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Fact]
        public void DoubleSignal()
        {
            SlimResume.Completed.Signal();
        }
    }
}
