using System.Text;
using PS3ISORebuilder.ISO9660;

namespace PS3.IsoRebuilder.ISO9660
{
    public class BaseVolumeDescriptor
    {
        public DescriptorType VolumeDescriptorType;

        public string StandardIdentifier;

        public byte Version;

        public Encoding GetEncoding => VolumeDescriptorType == DescriptorType.Supplementary
            ? Encoding.BigEndianUnicode
            : Encoding.ASCII;

        public BaseVolumeDescriptor(byte[] b)
        {
            VolumeDescriptorType = (DescriptorType)b[0];
            StandardIdentifier = Encoding.ASCII.GetString(b, 1, 5).Trim();
            Version = b[6];
        }
    }
}
