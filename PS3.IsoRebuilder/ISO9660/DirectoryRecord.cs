using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using PS3ISORebuilder.ISO9660;

namespace PS3.IsoRebuilder.ISO9660
{
    public class DirectoryRecord : Stream
    {
        public byte recordLength;

        public byte sectorsInExtendedRecord;

        public uint firstDataSector;

        public uint dataLength;

        public DateTime RecordingDateAndTime;

        public FileFlags flags;

        public byte fileUnitSize;

        public byte interleaveGap;

        public ushort volSeqNumber;

        public byte nameLength;

        public string name;

        public string fullname;

        public bool isDirectory;

        public byte FileVersion;

        public DirectoryRecord parent;

        public Dictionary<string, DirectoryRecord> directorys;

        public Dictionary<string, DirectoryRecord> files;

        public ulong _Length;

        public uint blocksize;

        private int currentSector;

        private long currentOffset;

        private int sectorOffset;

        private byte[] sectorBuffer;

        private PS3ISORebuilder.ISO9660.ISO9660 internalReader;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => checked((long)_Length);

        public override long Position
        {
            get => currentOffset;
            set => Seek(value, SeekOrigin.Begin);
        }

        public DirectoryRecord(byte[] buffer, int offset, Encoding enc, PS3ISORebuilder.ISO9660.ISO9660 reader)
        {
            name = "";
            fullname = "";
            FileVersion = 0;
            directorys = new Dictionary<string, DirectoryRecord>();
            files = new Dictionary<string, DirectoryRecord>();
            checked
            {
                recordLength = buffer[0 + offset];
                if (recordLength > 0)
                {
                    sectorsInExtendedRecord = buffer[1 + offset];
                    firstDataSector = BitConverter.ToUInt32(buffer, 2 + offset);
                    dataLength = BitConverter.ToUInt32(buffer, 10 + offset);
                    try
                    {
                        var dateTime = new DateTime(1900 + buffer[checked(18 + offset)], buffer[19 + offset], buffer[20 + offset], buffer[21 + offset], buffer[22 + offset], buffer[23 + offset]);
                        var dateTime2 = dateTime;
                        RecordingDateAndTime = dateTime2.AddHours(4 * buffer[checked(24 + offset)]);
                    }
                    catch (Exception projectError)
                    {
                        ProjectData.SetProjectError(projectError);
                        RecordingDateAndTime = DateTime.Now;
                        ProjectData.ClearProjectError();
                    }
                    unchecked
                    {
                        flags = (FileFlags)buffer[checked(25 + offset)];
                    }
                    fileUnitSize = buffer[26 + offset];
                    interleaveGap = buffer[27 + offset];
                    volSeqNumber = BitConverter.ToUInt16(buffer, 28 + offset);
                    nameLength = buffer[32 + offset];
                    if (nameLength == 1 && (buffer[33 + offset] == 0 || buffer[33 + offset] == 1))
                    {
                        name = buffer[33 + offset] == 0 ? "." : "..";
                    }
                    else
                    {
                        name = enc.GetString(buffer, 33 + offset, nameLength);
                        if (Strings.InStr(name, ";") != 0)
                        {
                            FileVersion = (byte)Conversions.ToInteger(Strings.Split(name, ";")[1]);
                            name = Strings.Split(name, ";")[0];
                        }
                    }
                    isDirectory = Conversions.ToBoolean(Interaction.IIf(flags == FileFlags.Directory, true, false));
                }
                _Length = dataLength;
                currentSector = (int)firstDataSector;
                currentOffset = 0L;
                sectorOffset = 0;
                internalReader = reader;
                blocksize = reader.Blocksize;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            checked
            {
                if (offset < 0 || count < 0 || count > buffer.Length - offset)
                {
                    return 0;
                }
                sectorBuffer = internalReader.ReadSector((ulong)currentSector);
                var num = 0;
                var num2 = ReadInternal(buffer, offset, (int)Math.Min(count, blocksize - sectorOffset));
                offset += num2;
                count -= num2;
                num += num2;
                
                while (count >= blocksize && decimal.Compare(new decimal(currentOffset), new decimal(_Length)) < 0)
                {
                    ReadNextSector();
                    var num3 = ReadInternal(buffer, offset, (int)blocksize);
                    offset += num3;
                    count -= num3;
                    num += num3;
                }

                if (count <= 0) return num;
                
                ReadNextSector();
                var num4 = ReadInternal(buffer, offset, count);
                num += num4;
                return num;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            checked
            {
                offset = origin switch
                {
                    SeekOrigin.Current => currentOffset + offset,
                    SeekOrigin.End => Length - offset,
                    _ => offset
                };
                
                if ((decimal.Compare(new decimal(offset), new decimal(_Length)) > 0) | (offset < 0))
                {
                    Console.WriteLine("Seek offset " + Conversions.ToString(offset) + " out of bounds.");
                    offset = 0L;
                }
                
                currentOffset = offset;
                currentSector = (int)Math.Round(firstDataSector + Conversion.Fix(offset / (double)blocksize));
                sectorOffset = (int)(offset % blocksize);
                return currentOffset;
            }
        }

        public override void SetLength(long value)
        {
            _Length = checked((ulong)value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException($"{nameof(DirectoryRecord)}.{nameof(Write)} is not implemented.");
        }

        private int ReadInternal(byte[] b, int off, int len)
        {
            checked
            {
                if (len <= 0) return len;
                
                if (decimal.Compare(new decimal(len), decimal.Subtract(new decimal(_Length), new decimal(currentOffset))) > 0)
                {
                    len = Convert.ToInt32(decimal.Subtract(new decimal(_Length), new decimal(currentOffset)));
                }
                
                Array.Copy(sectorBuffer, sectorOffset, b, off, len);
                sectorOffset += len;
                currentOffset += len;
                return len;
            }
        }

        private void ReadNextSector()
        {
            checked
            {
                if (sectorOffset != blocksize) return;
                
                currentSector++;
                
                if (currentSector >= internalReader.VolumeDescriptor.VolumeSpaceSize) return;
                
                sectorBuffer = internalReader.ReadSector((ulong)currentSector);
                sectorOffset = 0;
            }
        }

        public void Reset()
        {
            Seek(0L, SeekOrigin.Begin);
        }
    }
}
