#region AAAA data format
/*
2.2 AAAA data format

   A 128 bit IPv6 address is encoded in the data portion of an AAAA
   resource record in network byte order (high-order byte first).
 */
#endregion

namespace Zeroconf.DNS
{
    internal class RecordAAAA(RecordReader rr) : Record
    {
        public string Address =
            $"{rr.ReadUInt16():x}:{rr.ReadUInt16():x}:{rr.ReadUInt16():x}:{rr.ReadUInt16():x}:{rr.ReadUInt16():x}:{rr.ReadUInt16():x}:{rr.ReadUInt16():x}:{rr.ReadUInt16():x}";

        public override string ToString() => Address.ToString();
    }
}
