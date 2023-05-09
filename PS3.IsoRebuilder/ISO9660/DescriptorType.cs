namespace PS3.IsoRebuilder.ISO9660
{
    public enum DescriptorType : byte
    {
        BootRecord = 0,
        Primary = 1,
        Supplementary = 2,
        Partition = 3,
        Terminator = byte.MaxValue
    }
}
