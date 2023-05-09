using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using PS3.IsoRebuilder.ISO9660;

namespace PS3ISORebuilder.ISO9660
{
    public class ISO9660
    {
        private bool compresion;

        public uint Blocksize;

        private Stream internalstream;

        public ulong Disksize;

        private DirectoryRecord root;

        public VolumeDescriptor VolumeDescriptor;

        private Dictionary<DescriptorType, VolumeDescriptor> VolumeDescriptors;

        public Dictionary<string, DirectoryRecord> dirlist;

        public Dictionary<string, DirectoryRecord> filelist;

        public ISO9660(string filename)
        {
            compresion = false;
            Blocksize = 2048u;
            VolumeDescriptors = new Dictionary<DescriptorType, VolumeDescriptor>();
            dirlist = new Dictionary<string, DirectoryRecord>(StringComparer.OrdinalIgnoreCase);
            filelist = new Dictionary<string, DirectoryRecord>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(filename))
            {
                internalstream = File.OpenRead(filename);
                if (!Parse())
                {
                    Console.WriteLine("not a ISO File");
                }
            }
        }

        public ISO9660(Stream fstream)
        {
            compresion = false;
            Blocksize = 2048u;
            VolumeDescriptors = new Dictionary<DescriptorType, VolumeDescriptor>();
            dirlist = new Dictionary<string, DirectoryRecord>(StringComparer.OrdinalIgnoreCase);
            filelist = new Dictionary<string, DirectoryRecord>(StringComparer.OrdinalIgnoreCase);
            if (fstream != null)
            {
                internalstream = fstream;
                if (!Parse())
                {
                    Console.WriteLine("not a ISO File");
                }
            }
        }

        private bool Parse()
        {
            var array = new byte[16];
            internalstream.Seek(0L, SeekOrigin.Begin);
            internalstream.Read(array, 0, array.Length);
            checked
            {
                if (Operators.CompareString(Encoding.Default.GetString(array, 0, 4), "CPS3", TextCompare: false) == 0)
                {
                    compresion = true;
                    var num = BitConverter.ToUInt64(array, 4);
                    Blocksize = BitConverter.ToUInt32(array, 12);
                    Disksize = num * Blocksize;
                }
                try
                {
                    var num2 = 16;
                    var flag = true;
                    while (flag)
                    {
                        var b = ReadSector((ulong)num2);
                        var baseVolumeDescriptor = new BaseVolumeDescriptor(b);
                        var standardIdentifier = baseVolumeDescriptor.StandardIdentifier;
                        if (Operators.CompareString(standardIdentifier, "CD001", TextCompare: false) == 0)
                        {
                            if (!VolumeDescriptors.ContainsKey(baseVolumeDescriptor.VolumeDescriptorType))
                            {
                                VolumeDescriptors.Add(baseVolumeDescriptor.VolumeDescriptorType, new VolumeDescriptor(b, this));
                            }
                        }
                        else
                        {
                            flag = false;
                        }
                        num2++;
                    }
                    if (VolumeDescriptors.ContainsKey(DescriptorType.Supplementary))
                    {
                        VolumeDescriptor = VolumeDescriptors[DescriptorType.Supplementary];
                    }
                    else
                    {
                        if (!VolumeDescriptors.ContainsKey(DescriptorType.Primary))
                        {
                            return false;
                        }
                        VolumeDescriptor = VolumeDescriptors[DescriptorType.Primary];
                    }
                    Blocksize = (uint)(long)VolumeDescriptor.LogicalBlockSize;
                    Disksize = (ulong)((long)VolumeDescriptor.VolumeSpaceSize * (long)Blocksize);
                    root = VolumeDescriptor.DirectoryRecord;
                    root.name = "\\";
                    root.fullname = "\\";
                    dirlist.Add(root.fullname, root);
                    ReadDirectoryRecord(root);
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

        public void close()
        {
            internalstream.Close();
        }

        public void ReadDirectoryRecord(DirectoryRecord root)
        {
            try
            {
                if ((long)root.dataLength <= 0L) return;
                
                var num = 0L;
                checked
                {
                    var buffer = new byte[(int)((long)root.dataLength - 1L) + 1];
                    root.Read(buffer, 0, (int)root.dataLength);
                    root.Reset();
                    while (num > -1)
                    {
                        var directoryRecord = new DirectoryRecord(buffer, (int)num, VolumeDescriptor.GetEncoding, this);
                        directoryRecord.parent = root;
                        if (directoryRecord.recordLength > 0)
                        {
                            if (!((Operators.CompareString(directoryRecord.name, ".", TextCompare: false) == 0) | (Operators.CompareString(directoryRecord.name, "..", TextCompare: false) == 0)))
                            {
                                directoryRecord.fullname = Path.Combine(root.fullname, directoryRecord.name);
                                if (directoryRecord.flags == FileFlags.Directory)
                                {
                                    dirlist.TryAdd(directoryRecord.fullname, directoryRecord);
                                    root.directorys.TryAdd(directoryRecord.fullname, directoryRecord);
                                    ReadDirectoryRecord(directoryRecord);
                                }
                                else if (filelist.ContainsKey(directoryRecord.fullname))
                                {
                                    filelist[directoryRecord.fullname].SetLength(filelist[directoryRecord.fullname].Length + directoryRecord.Length);
                                }
                                else
                                {
                                    filelist.Add(directoryRecord.fullname, directoryRecord);
                                    if (root.files.ContainsKey(directoryRecord.fullname))
                                    {
                                        root.files.Add(directoryRecord.fullname, directoryRecord);
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

        public ulong ReadSectorOffset(ulong sector)
        {
            internalstream.Seek(Convert.ToInt64(decimal.Add(decimal.Multiply(new decimal(sector), new decimal(8L)), new decimal(16L))), SeekOrigin.Begin);
            var array = new byte[8];
            internalstream.Read(array, 0, array.Length);
            return BitConverter.ToUInt64(array, 0);
        }

        public byte[] ReadSector(ulong SectorNumber)
        {
            checked
            {
                var array = new byte[(int)((long)Blocksize - 1L) + 1];
                if (!compresion)
                {
                    internalstream.Seek((long)(Blocksize * SectorNumber), SeekOrigin.Begin);
                    internalstream.Read(array, 0, array.Length);
                    return array;
                }
                var num = ReadSectorOffset(SectorNumber);
                var num2 = ReadSectorOffset(Convert.ToUInt64(decimal.Add(new decimal(SectorNumber), decimal.One)));
                var num3 = (int)(num2 - num);
                if (num3 == Blocksize)
                {
                    internalstream.Seek((long)num, SeekOrigin.Begin);
                    internalstream.Read(array, 0, array.Length);
                    return array;
                }
                var array2 = new byte[num3 - 1 + 1];
                internalstream.Seek((long)num, SeekOrigin.Begin);
                internalstream.Read(array2, 0, array2.Length);
                var num4 = new DeflateStream(new MemoryStream(array2), CompressionMode.Decompress, leaveOpen: false).Read(array, 0, (int)Blocksize);
                return array;
            }
        }

        public object DirExist(string dirname)
        {
            return dirlist.ContainsKey(dirname);
        }

        public bool FileExist(string filename)
        {
            return filelist.ContainsKey(filename);
        }

        public DirectoryRecord FindFile(string filename)
        {
            return FileExist(filename) ? filelist[filename] : null;
        }
    }
}
