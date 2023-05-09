namespace PS3.IsoRebuilder.IrdFile
{
    public class CRC32
    {
        private readonly int[] _crc32Table;

        private const int BufferSize = 1024;

        public int GetCrc32(ref Stream stream, long lenght)
        {
            stream.Seek(0L, SeekOrigin.Begin);
            var array = new byte[checked((int)(lenght - 1) + 1)];
            stream.Read(array, 0, array.Length);
            return GetCrc32(new MemoryStream(array));
        }

        public int GetCrc32(Stream stream)
        {
            stream.Seek(0L, SeekOrigin.Begin);
            var num = -1;
            var array = new byte[1025];
            var count = 1024;
            var num2 = stream.Read(array, 0, count);
            while (num2 > 0)
            {
                var num4 = checked(num2 - 1);
                for (var i = 0; i <= num4; i = checked(i + 1))
                {
                    var num5 = (num & 0xFF) ^ array[i];
                    num = (((num & -256) / 256) & 0xFFFFFF);
                    num ^= _crc32Table[num5];
                }
                num2 = stream.Read(array, 0, count);
            }
            return ~num;
        }

        public CRC32()
        {
            var num = -306674912;
            _crc32Table = new int[257];
            var num2 = 0;
            checked
            {
                do
                {
                    var num3 = num2;
                    var num4 = 8;
                    do
                    {
                        if ((num3 & 1) != 0)
                        {
                            num3 = (int)(unchecked((long)(num3 & -2) / 2L) & int.MaxValue);
                            num3 ^= num;
                        }
                        else
                        {
                            num3 = (int)(unchecked((long)(num3 & -2) / 2L) & int.MaxValue);
                        }
                        num4 += -1;
                    }
                    while (num4 >= 1);
                    _crc32Table[num2] = num3;
                    num2++;
                }
                while (num2 <= 255);
            }
        }
    }
}
