using Microsoft.VisualBasic.CompilerServices;
using PS3ISORebuilder.IRDFile;

namespace PS3.IsoRebuilder.IrdFile
{
    public class Iso
    {
        public long Blocksize;

        private readonly Stream _internalreader;

        public VolumeDescriptor VolumeDescriptor;

        public readonly Dictionary<DescriptorType, VolumeDescriptor> VolumeDescriptors;

        public readonly Dictionary<string, DirectoryRecord> Dirlist;

        public readonly Dictionary<string, DirectoryRecord> Filelist;

        private readonly Dictionary<long, byte[]> _filehashes;

        private DirectoryRecord _root;

        public ulong Disksize;

        public Iso(Stream fstream, Dictionary<long, byte[]> hashes)
        {
            Blocksize = 2048L;
            VolumeDescriptors = new Dictionary<DescriptorType, VolumeDescriptor>();
            Dirlist = new Dictionary<string, DirectoryRecord>(StringComparer.OrdinalIgnoreCase);
            Filelist = new Dictionary<string, DirectoryRecord>(StringComparer.OrdinalIgnoreCase);
            _filehashes = hashes;
            
            _internalreader = fstream;
            if (!Parse())
            {
                Console.WriteLine("not a ISO File");
            }
        }

        private bool Parse()
        {
            checked
            {
                try
                {
                    var num = 16;
                    var flag = true;
                    while (flag)
                    {
                        var b = Readsector((ulong)num, (ulong)Blocksize);
                        var baseVolumeDescriptor = new BaseVolumeDescriptor(b);
                        var standardIdentifier = baseVolumeDescriptor.StandardIdentifier;
                        if (Operators.CompareString(standardIdentifier, "CD001", TextCompare: false) == 0)
                        {
                            if (!VolumeDescriptors.ContainsKey(baseVolumeDescriptor.VolumeDescriptorType))
                            {
                                VolumeDescriptors.Add(baseVolumeDescriptor.VolumeDescriptorType, new VolumeDescriptor(b));
                            }
                        }
                        else
                        {
                            flag = false;
                        }
                        num++;
                    }
                    if (VolumeDescriptors.TryGetValue(DescriptorType.Supplementary, out var descriptor))
                    {
                        VolumeDescriptor = descriptor;
                    }
                    else
                    {
                        if (!VolumeDescriptors.ContainsKey(DescriptorType.Primary))
                        {
                            return false;
                        }
                        VolumeDescriptor = VolumeDescriptors[DescriptorType.Primary];
                    }
                    Blocksize = VolumeDescriptor.LogicalBlockSize;
                    Disksize = (ulong)((long)VolumeDescriptor.VolumeSpaceSize * Blocksize);
                    _root = VolumeDescriptor.DirectoryRecord;
                    _root.name = "\\";
                    _root.entrypath = "\\";
                    Dirlist.Add(_root.entrypath, _root);
                    ReadDirectoryRecord(_root);
                    return true;
                }
                catch (Exception projectError)
                {
                    ProjectData.SetProjectError(projectError);
                    var result = false;
                    ProjectData.ClearProjectError();
                    return result;
                }
            }
        }

        public void ReadDirectoryRecord(DirectoryRecord root)
        {
            try
            {
                if ((long)root.dataLength <= 0L) return;
                var num = 0L;
                var buffer = Readsector(root.firstDataSector, root.dataLength);
                checked
                {
                    while (num > -1)
                    {
                        var directoryRecord = new DirectoryRecord(buffer, (int)num, VolumeDescriptor.getencoding)
                        {
                            parent = root
                        };
                        
                        if (directoryRecord.recordLength > 0)
                        {
                            if (!((Operators.CompareString(directoryRecord.name, ".", TextCompare: false) == 0) | (Operators.CompareString(directoryRecord.name, "..", TextCompare: false) == 0)))
                            {
                                directoryRecord.entrypath = Path.Combine(root.entrypath, directoryRecord.name);
                                if (directoryRecord.flags == FileFlags.Directory)
                                {
                                    Dirlist.TryAdd(directoryRecord.entrypath, directoryRecord);
                                    root.directorys.TryAdd(directoryRecord.entrypath, directoryRecord);
                                    ReadDirectoryRecord(directoryRecord);
                                }
                                else if (Filelist.TryGetValue(directoryRecord.entrypath, out var directoryRecord2))
                                {
                                    directoryRecord2.Length += directoryRecord.dataLength;
                                }
                                else
                                {
                                    directoryRecord.md5 = _filehashes[directoryRecord.firstDataSector];
                                    Filelist.Add(directoryRecord.entrypath, directoryRecord);
                                    if (root.files.ContainsKey(directoryRecord.entrypath))
                                    {
                                        root.files.Add(directoryRecord.entrypath, directoryRecord);
                                    }
                                }
                            }
                        }
                        else
                        {
                            directoryRecord.recordLength = 1;
                        }
                        num += (long)directoryRecord.recordLength;
                        if (num >= root.dataLength)
                        {
                            num = -1L;
                        }
                    }
                }
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                ProjectData.ClearProjectError();
            }
        }

        public byte[] Readsector(ulong sectorNumber, ulong length)
        {
            var array = new byte[checked(Convert.ToInt32(decimal.Subtract(new decimal(length), decimal.One)) + 1)];
            _internalreader.Seek(Convert.ToInt64(decimal.Multiply(new decimal(Blocksize), new decimal(sectorNumber))), SeekOrigin.Begin);
            _ = _internalreader.Read(array, 0, array.Length);
            return array;
        }
    }
}
