using System;
using Xunit;
using async_enumerable_dotnet;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;


[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace async_enumerable_dotnet_test
{
    public class AsyncEnumerableTest
    {

        [Fact]
        public void NullChecks()
        {
            foreach (var m2 in typeof(AsyncEnumerable).GetMethods())
            {
                if (m2.IsStatic && m2.IsPublic)
                {
                    var m = default(MethodInfo);

                    if (m2.IsGenericMethod)
                    {
                        Type[] argtypes = m2.GetGenericArguments();
                        var gargs = new Type[argtypes.Length];
                        
                        for (int i = 0; i < argtypes.Length; i++)
                        {
                            var argt = argtypes[i];

                            var gconst = argt.GetGenericParameterConstraints();

                            if (gconst.Length == 0)
                            {
                                gargs[i] = typeof(int);
                            } else
                            if (gconst[0].Name.Contains("ICollection"))
                            {
                                gargs[i] = typeof(ICollection<int>);
                            }
                            else
                            {
                                Assert.False(true, "Method generic parameter default missing: " + argt);
                            }
                        }

                        m = m2.MakeGenericMethod(gargs);
                    }
                    else
                    {
                        m = m2;
                    }

                    var args = m.GetParameters();

                    for (int i = 0; i < args.Length; i++)
                    {
                        var arg = args[i];

                        if ((arg.ParameterType.IsClass || arg.ParameterType.IsInterface) && !arg.HasDefaultValue)
                        {
                            var pars = new object[args.Length];
                            for (int j = 0; j < args.Length; j++)
                            {
                                if (j != i)
                                {
                                    pars[j] = GetDefault(args[j].ParameterType, m);
                                }
                            }

                            var thrown = false;
                            try
                            {
                                m.Invoke(null, pars);
                            }
                            catch (TargetInvocationException ex)
                            {
                                if (ex.InnerException is ArgumentNullException)
                                {
                                    thrown = true;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                            if (!thrown)
                            {
                                Assert.False(true, "Method " + m.Name + " argument " + arg.Name + " should have thrown!");
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void RequirePositiveInt0()
        {
            try
            {
                AsyncEnumerable.RequirePositive(0, "param");
                Assert.False(true, "Should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
                // expected
            }
        }

        [Fact]
        public void RequireNonNull_Null()
        {
            try
            {
                AsyncEnumerable.RequireNonNull<string>(null, "param");
                Assert.False(true, "Should have thrown");
            }
            catch (ArgumentNullException)
            {
                // expected
            }
        }

        [Fact]
        public void RequirePositiveInt_Minus1()
        {
            try
            {
                AsyncEnumerable.RequirePositive(-1, "param");
                Assert.False(true, "Should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
                // expected
            }
        }

        [Fact]
        public void RequirePositiveLong0()
        {
            try
            {
                AsyncEnumerable.RequirePositive(0L, "param");
                Assert.False(true, "Should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
                // expected
            }
        }

        [Fact]
        public void RequirePositiveLong_Minus1()
        {
            try
            {
                AsyncEnumerable.RequirePositive(-1L, "param");
                Assert.False(true, "Should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
                // expected
            }
        }

        [Fact]
        public void RequirePositiveLong_Plus1()
        {
            AsyncEnumerable.RequirePositive(1L, "param");
        }

        private static readonly Dictionary<Type, object> Defaults = new Dictionary<Type, object>();

        static AsyncEnumerableTest()
        {
            Defaults.Add(typeof(bool), true);

            Defaults.Add(typeof(int), 1);
            Defaults.Add(typeof(int[]), new[] { 1 });
            Defaults.Add(typeof(long), 1L);

            Defaults.Add(typeof(IEnumerable<int>), Enumerable.Empty<int>());
            Defaults.Add(typeof(IAsyncEnumerable<int>), AsyncEnumerable.Empty<int>());
            Defaults.Add(typeof(IAsyncEnumerable<int>[]), new[] { AsyncEnumerable.Empty<int>() });

            Defaults.Add(typeof(Func<int>), (Func<int>)(() => 1));
            Defaults.Add(typeof(Func<int, bool>), (Func<int, bool>)(v => true));
            Defaults.Add(typeof(Func<int, int>), (Func<int, int>)(v => v));
            Defaults.Add(typeof(Func<int, int, int>), (Func<int, int, int>)((v, w) => v));
            Defaults.Add(typeof(Func<int[], int>), (Func<int[], int>)(v => v[0]));
            Defaults.Add(typeof(Func<IAsyncEnumerable<int>>), (Func<IAsyncEnumerable<int>>)(() => AsyncEnumerable.Empty<int>()));
            Defaults.Add(typeof(Func<int, IEnumerable<int>>), (Func<int, IEnumerable<int>>)(v => Enumerable.Empty<int>()));
            Defaults.Add(typeof(Func<int, IAsyncEnumerable<int>>), (Func<int, IAsyncEnumerable<int>>)(v => AsyncEnumerable.Empty<int>()));
            Defaults.Add(typeof(Func<Exception, IAsyncEnumerable<int>>), (Func<Exception, IAsyncEnumerable<int>>)(v => AsyncEnumerable.Empty<int>()));
            Defaults.Add(typeof(Func<int, Task>), (Func<int, Task>)(v => null));
            Defaults.Add(typeof(Func<int, Task<int>>), (Func<int, Task<int>>)(v => null));
            Defaults.Add(typeof(Func<int, Task<bool>>), (Func<int, Task<bool>>)(v => null));
            Defaults.Add(typeof(Func<ICollection<int>>), (Func<ICollection<int>>)(() => null));
            Defaults.Add(typeof(Func<Task>), (Func<Task>)(() => null));
            Defaults.Add(typeof(Func<ValueTask>), (Func<ValueTask>)(() => new ValueTask()));
            Defaults.Add(typeof(Func<long, bool>), (Func<long, bool>)(v => false));
            Defaults.Add(typeof(Func<long, Exception, bool>), (Func<long, Exception, bool>)((v, w) => false));

            Defaults.Add(typeof(Action), (Action)(() => { }));
            Defaults.Add(typeof(Action<int>), (Action<int>)(v => { }));
            Defaults.Add(typeof(Action<int, int>), (Action<int, int>)((v, w) => { }));
            Defaults.Add(typeof(Action<Exception>), (Action<Exception>)(v => { }));

            Defaults.Add(typeof(IEqualityComparer<int>), EqualityComparer<int>.Default);

            Defaults.Add(typeof(IAsyncConsumer<int>), new EmptyAsyncConsumer());

            Defaults.Add(typeof(CancellationToken), new CancellationToken());

            Defaults.Add(typeof(TimeSpan), TimeSpan.FromMilliseconds(1));
        }

        static object GetDefault(Type type, MethodInfo m)
        {
            if (!Defaults.ContainsKey(type))
            {
                Assert.False(true, "No default for " + type + " \r\n\r\n Method: " + m);
            }
            return Defaults[type];
        }

        sealed class EmptyAsyncConsumer : IAsyncConsumer<int>
        {
            public ValueTask Complete()
            {
                return new ValueTask();
            }

            public ValueTask Error(Exception ex)
            {
                return new ValueTask();
            }

            public ValueTask Next(int value)
            {
                return new ValueTask();
            }
        }
    }
}
