using System.Text;
using PS3ISORebuilder.IRDFile;

namespace PS3.IsoRebuilder.IrdFile
{
    public class BaseVolumeDescriptor
    {
        public DescriptorType VolumeDescriptorType;

        public string StandardIdentifier;

        public byte Version;

        public Encoding getencoding
        {
            get
            {
                if (VolumeDescriptorType == DescriptorType.Supplementary)
                {
                    return Encoding.BigEndianUnicode;
                }
                return Encoding.ASCII;
            }
        }

        public BaseVolumeDescriptor(byte[] b)
        {
            VolumeDescriptorType = (DescriptorType)b[0];
            StandardIdentifier = Encoding.ASCII.GetString(b, 1, 5).Trim();
            Version = b[6];
        }
    }
}
