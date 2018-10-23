using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace async_enumerable_dotnet
{
    public interface IAsyncEmitter<in T>
    {
        ValueTask Next(T value);

        bool DisposeAsyncRequested { get; }
    }

    /// <summary>
    /// The "push-side" of an IAsyncEnumerator.
    /// Each method should be awaited and called non-overlappingly.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public interface IAsyncConsumer<in T>
    {
        ValueTask Next(T value);

        ValueTask Error(Exception ex);

        ValueTask Complete();
    }
}
