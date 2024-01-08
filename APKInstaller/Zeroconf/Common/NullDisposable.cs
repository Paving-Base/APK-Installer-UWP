using System;

namespace Zeroconf.Common
{
    internal readonly struct NullDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
