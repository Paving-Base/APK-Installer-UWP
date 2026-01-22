#region A RDATA format
/*
 3.4.1. A RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    ADDRESS                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

ADDRESS         A 32 bit Internet address.

Hosts that have multiple Internet addresses will have multiple A
records.
 * 
 */
#endregion

namespace Zeroconf.DNS
{
    internal class RecordA(RecordReader rr) : Record
    {
        public string Address =
            $"{rr.ReadByte()}.{rr.ReadByte()}.{rr.ReadByte()}.{rr.ReadByte()}";

        public override string ToString() => Address;
    }
}
