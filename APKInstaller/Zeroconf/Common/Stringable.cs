using System;
using Windows.Foundation;

namespace Zeroconf.Common
{
    internal readonly struct Stringable(Func<string> toString) : IStringable
    {
        public override string ToString() => toString();
    }
}
