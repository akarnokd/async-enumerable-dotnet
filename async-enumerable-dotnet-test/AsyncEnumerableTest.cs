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

        static readonly Dictionary<Type, object> defaults = new Dictionary<Type, object>();

        static AsyncEnumerableTest()
        {
            defaults.Add(typeof(bool), true);

            defaults.Add(typeof(int), 1);
            defaults.Add(typeof(int[]), new[] { 1 });
            defaults.Add(typeof(long), 1L);

            defaults.Add(typeof(IEnumerable<int>), Enumerable.Empty<int>());
            defaults.Add(typeof(IAsyncEnumerable<int>), AsyncEnumerable.Empty<int>());
            defaults.Add(typeof(IAsyncEnumerable<int>[]), new[] { AsyncEnumerable.Empty<int>() });

            defaults.Add(typeof(Func<int>), (Func<int>)(() => 1));
            defaults.Add(typeof(Func<int, bool>), (Func<int, bool>)(v => true));
            defaults.Add(typeof(Func<int, int>), (Func<int, int>)(v => v));
            defaults.Add(typeof(Func<int, int, int>), (Func<int, int, int>)((v, w) => v));
            defaults.Add(typeof(Func<int[], int>), (Func<int[], int>)(v => v[0]));
            defaults.Add(typeof(Func<IAsyncEnumerable<int>>), (Func<IAsyncEnumerable<int>>)(() => AsyncEnumerable.Empty<int>()));
            defaults.Add(typeof(Func<int, IEnumerable<int>>), (Func<int, IEnumerable<int>>)(v => Enumerable.Empty<int>()));
            defaults.Add(typeof(Func<int, IAsyncEnumerable<int>>), (Func<int, IAsyncEnumerable<int>>)(v => AsyncEnumerable.Empty<int>()));
            defaults.Add(typeof(Func<Exception, IAsyncEnumerable<int>>), (Func<Exception, IAsyncEnumerable<int>>)(v => AsyncEnumerable.Empty<int>()));
            defaults.Add(typeof(Func<int, Task>), (Func<int, Task>)(v => null));
            defaults.Add(typeof(Func<int, Task<int>>), (Func<int, Task<int>>)(v => null));
            defaults.Add(typeof(Func<int, Task<bool>>), (Func<int, Task<bool>>)(v => null));
            defaults.Add(typeof(Func<ICollection<int>>), (Func<ICollection<int>>)(() => null));
            defaults.Add(typeof(Func<Task>), (Func<Task>)(() => null));
            defaults.Add(typeof(Func<ValueTask>), (Func<ValueTask>)(() => new ValueTask()));
            defaults.Add(typeof(Func<long, bool>), (Func<long, bool>)(v => false));
            defaults.Add(typeof(Func<long, Exception, bool>), (Func<long, Exception, bool>)((v, w) => false));

            defaults.Add(typeof(Action), (Action)(() => { }));
            defaults.Add(typeof(Action<int>), (Action<int>)(v => { }));
            defaults.Add(typeof(Action<int, int>), (Action<int, int>)((v, w) => { }));
            defaults.Add(typeof(Action<Exception>), (Action<Exception>)(v => { }));

            defaults.Add(typeof(IEqualityComparer<int>), EqualityComparer<int>.Default);

            defaults.Add(typeof(IAsyncConsumer<int>), new EmptyAsyncConsumer());

            defaults.Add(typeof(CancellationToken), new CancellationToken());

            defaults.Add(typeof(TimeSpan), TimeSpan.FromMilliseconds(1));
        }

        static object GetDefault(Type type, MethodInfo m)
        {
            if (!defaults.ContainsKey(type))
            {
                Assert.False(true, "No default for " + type + " \r\n\r\n Method: " + m);
            }
            return defaults[type];
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
