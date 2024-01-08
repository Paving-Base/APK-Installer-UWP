namespace Zeroconf.DNS
{
    internal class RecordNSEC : Record
    {
        public byte[] RDATA;

        public RecordNSEC(RecordReader rr)
        {
            // re-read length
            ushort RDLENGTH = rr.ReadUInt16(-2);

            RDATA = rr.ReadBytes(RDLENGTH);
        }

        public override string ToString() => string.Format("not-used");
    }
}
