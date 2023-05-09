namespace PS3.IsoRebuilder.IrdFile
{
    public enum FileFlags : byte
    {
        None = 0,
        Hidden = 1,
        Directory = 2,
        AssociatedFile = 4,
        Record = 8,
        Protection = 0x10,
        MultiExtent = 0x80
    }
}
