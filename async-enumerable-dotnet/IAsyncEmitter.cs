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
}
