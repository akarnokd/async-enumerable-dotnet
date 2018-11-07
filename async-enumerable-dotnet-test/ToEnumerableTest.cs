// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;
using async_enumerable_dotnet;
using System.Linq;

namespace async_enumerable_dotnet_test
{
    public class ToEnumerableTest
    {
        [Fact]
        public void Normal()
        {
            var list = AsyncEnumerable.Range(1, 5).ToEnumerable().ToList();

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list);
        }

        [Fact]
        public void NonGeneric()
        {
            IEnumerable en = AsyncEnumerable.Range(1, 5).ToEnumerable();

            var enumerator = en.GetEnumerator();

            for (var i = 1; i <= 5; i++)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(i, enumerator.Current);
            }
            Assert.False(enumerator.MoveNext());

            try
            {
                enumerator.Reset();
                Assert.False(true, "Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }
    }
}
