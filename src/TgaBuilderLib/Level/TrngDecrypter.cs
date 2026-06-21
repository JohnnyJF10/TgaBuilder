namespace TgaBuilderLib.Level
{
    public class TrngDecrypter : ITrngDecrypter
    {
        private readonly string EncryptionKeysStr =
            "OCLpTyDxN2kVE9OQF4TCCa0EvtJSGAvsRzioFI2ql9DfKZKZyPjecL0afNu/iydTVk5nHfoes/LvP1FeNbD71ogvO6AtQ4e6Im" +
            "Ql4WjVz1r+VA8joiRcPpiJ+WXdt6PdMXrjptRKMMDKas3oENx4pKeftypLbwREQI+hr4YCTCjZgIzwfZTYx3K0MjO44iGrDKUc" +
            "dPN1rI7olge16gj0LoLK8PcmmoESG7vaOWEtybG0rMR7nL8RlZHgrryTD501K9GZ/c6h5RnDwYODCjRx+132bcUBxrmb9edXBz" +
            "xZRn/S10FJd+uOH2wNLIqyfhoDBfwWy6nMYuWe7f8O5F/uhTgi6U8g8TdpFRPTkBeEwgmtBL7SUhgL7Ec4qBSNqpfQ3ymSmcj4" +
            "3nC9Gnzbv4snU1ZOZx36HrPy7z9RXjWw+9aILzugLUOHuiJkJeFo1c9a/lQPI6IkXD6Yifll3bej3TF646bUSjDAymrN6BDceK" +
            "Snn7cqS28ERECPoa+GAkwo2YCM8H2U2MdytDIzuOIhqwylHHTzdayO6JYHteoI9C6CyvD3JpqBEhu72jlhLcmxtKzEe5y/EZWR" +
            "4K68kw+dNSvRmf3OoeUZw8GDgwo0cftd9m3FAca5m/XnVwc8WUZ/0tdBSXfrjh9sDSyKsn4aAwX8FsupzGLlnu3/DuRf7oU4Iu" +
            "lPIPE3aRUT05AXhMIJrQS+0lIYC+xHOKgUjaqX0N8pkpnI+N5wvRp827+LJ1NWTmcd+h6z8u8/UV41sPvWiC87oC1Dh7oiZCXh" +
            "aNXPWv5UDyOiJFw+mIn5Zd23o90xeuOm1EowwMpqzegQ3Hikp5+3KktvBERAj6GvhgJMKNmAjPB9lNjHcrQyM7jiIasMpRx083" +
            "WsjuiWB7XqCPQugsrw9yaagRIbu9o5YS3JsbSsxHucvxGVkeCuvJMPnTUr0Zn9zqHlGcPBg4MKNHH7XfZtxQHGuZv151cHPFlG" +
            "f9LXQUl3644fbA0sirJ+GgMF/BbLqcxi5Z7t/w7kX+6FOCLpTyDxN2kVE9OQF4TCCa0EvtJSGAvsRzioFI2ql9DfKZKZyPjecL" +
            "0afNu/iydTVk5nHfoes/LvP1FeNbD71ogvO6AtQ4e6ImQl4WjVz1r+VA8joiRcPpiJ+WXdt6PdMXrjptRKMMDKas3oENx4pKef" +
            "typLbwS8uAgaKP55xJ9R+ARp9QxQP+otqqoxW5gjhB6U7GztJQdhDn4uY4BtpvlDaG+eE/iKkzNSsdmlQSosJD3yFTiJDQpZJz" +
            "UMhhasokkRdkcZXZA8Ovr7gavpdNRu5T15PjIUbl/Pf7TQvfdLULnA7mMGl+SEowIq9pJ6fXWNRCFF2l4XZniFXNdm/a+aYseX" +
            "aq7hjItMCY78O4EmezZKyY+CZb6wIIwFIg9IWKALEkBwV+c1kfRTNwSfy87G3pVylitrZ7bI1q0pc08Bp7IYpLv/MpnbnVrfTk" +
            "jRd8yHmxuc07UQAXHcVi8cVanxWx9Nwag5QuJGYIhU7x0fFzChw+Z8vLgIGij+ecSfUfgEafUMUD/qLaqqMVuYI4QelOxs7SUH" +
            "YQ5+LmOAbab5Q2hvnhP4ipMzUrHZpUEqLCQ98hU4iQ0KWSc1DIYWrKJJEXZHGV2QPDr6+4Gr6XTUbuU9eT4yFG5fz3+00L33S1" +
            "C5wO5jBpfkhKMCKvaSen11jUQhRdpeF2Z4hVzXZv2vmmLHl2qu4YyLTAmO/DuBJns2SsmPgmW+sCCMBSIPSFigCxJAcFfnNZH0" +
            "UzcEn8vOxt6VcpYra2e2yNatKXNPAaeyGKS7/zKZ251a305I0XfMh5sbnNO1EAFx3FYvHFWp8VsfTcGoOULiRmCIVO8dHxcwoc" +
            "PmfLy4CBoo/nnEn1H4BGn1DFA/6i2qqjFbmCOEHpTsbO0lB2EOfi5jgG2m+UNob54T+IqTM1Kx2aVBKiwkPfIVOIkNClknNQyG" +
            "FqyiSRF2RxldkDw6+vuBq+l01G7lPXk+MhRuX89/tNC990tQucDuYwaX5ISjAir2knp9dY1EIUXaXhdmeIVc12b9r5pix5dqru" +
            "GMi0wJjvw7gSZ7NkrJj4JlvrAgjAUiD0hYoAsSQHBX5zWR9FM3BJ/L";

        private readonly string ObfuscationMapsStr =
            "ABEHCQYZARgwGgsWCBMVEhssFwojKyEdHikDHAwoJiQnDQ4uBS0iMR8qAhAEFA8vICUSIA8ABw0OEDArCCEoFhcpBiweMQodJh" +
            "gjHy4FJAMTIhUaDAEJJS0qLxQnBAsRAhkcGyYYDAAnLwcQFwgZHQ4aDywtJQsJFQUeKiEkFCgpAwYTAREWIiMCHzAuEjEgCisE" +
            "GxwNLQomBRcREyEEKh0YBhIxHiQAGQ4HHCIoEAggHwsbFBUjAQkuGiksDxYrAi8MMA0lJwMbBAorDAIWEhcHHhEiARgcKgshIC" +
            "MfDywUKCQZCS4QGgANKQgGAyYlFTEnEx0tLw4wBQYXFSgIEgMaJw4rLyMWCgcgLQ8sHx0kECEAHAEpCwIYBSYlBBEZIiouEwwx" +
            "GzAJHhQNBiUQFwsSJxUoACEbKgQNJBQwAxkHKQ4ICg8tJissIgwCHS8YHC4FAR4WExojCSARMR8qGw8HIQ4UEwwlEQALGR0KFQ" +
            "ggBRgeBBwiEi0rJAkoKSwBFw0vJwMmFjAaIy4QMQYfAhsODAkoAQIEKRcTIQgWFRgKLxkeJB0iKwYmBSAjCxQSMCoHMR8PEBot" +
            "AyUsAA0cLicRHQALBBASDRQZICwBHiUHBgkaFgMqISMkHw8CDiITJy4tKQgVMQoRJi8cKCsbBRcwGAwHGAoUBhwJECkmKzAvIx" +
            "YIEyADGyweJCEiLh0RJwIFGQQAJSgSGg4qDwsBMS0NHxUMFwYlEBcLEicVKAAhGyoEDSQUMAMZBykOCAoPLSYrLCIMAh0vGBwu" +
            "BQEeFhMaIwkgETEfKhsPByEOFBMMJREACxkdChUIIAUYHgQcIhItKyQJKCksARcNLycDJhYwGiMuEDEGHwIbDgwJKAECBCkXEy" +
            "EIFhUYCi8ZHiQdIisGJgUgIwsUEjAqBzEfDxAaLQMlLAANHC4nER0ACwQQEg0UGSAsAR4lBwYJGhYDKiEjJB8PAg4iEycuLSkI" +
            "FTEKESYvHCgrGwUXMBgMBxgKFAYcCRApJiswLyMWCBMgAxssHiQhIi4dEScCBRkEACUoEhoOKg8LATEtDR8VDBc=";

        private readonly byte[] EncryptionKeysData;
        private readonly byte[] ObfuscationMapsData;

        private const int KeyLength = 99;
        private const int MapCount = 16;
        private const int MapSize = 50;


        // Key offsets - precomputed offsets for each key
        private readonly int[] KeyOffsets = new int[MapCount];

        // Map offsets - precomputed offsets for each map
        private readonly int[] MapOffsets = new int[MapCount];

        public TrngDecrypter()
        {
            // Initialize offsets
            for (int i = 0; i < MapCount; i++)
            {
                KeyOffsets[i] = i * KeyLength;
                MapOffsets[i] = i * MapSize;
            }

            EncryptionKeysData = Convert.FromBase64String(EncryptionKeysStr);
            ObfuscationMapsData = Convert.FromBase64String(ObfuscationMapsStr);
        }

        public bool DecryptLevel(string source, string target)
        {
            if (!File.Exists(source))
                return false;

            byte[] header = new byte[KeyLength];

            try
            {
                if (File.Exists(target)) File.Delete(target);
                File.Copy(source, target);

                using (var iStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var oStream = new FileStream(target, FileMode.Open, FileAccess.Write, FileShare.None))
                using (var reader = new BinaryReader(iStream))
                using (var writer = new BinaryWriter(oStream))
                {
                    // Skip signature modification (original code writes 0 at position 3)
                    writer.Seek(3, SeekOrigin.Begin);
                    writer.Write((byte)0);

                    // Position both streams at the data start
                    const int dataStart = 14;
                    reader.BaseStream.Seek(dataStart, SeekOrigin.Begin);
                    writer.BaseStream.Seek(dataStart, SeekOrigin.Begin);

                    // Process 4 chunks
                    for (int chunk = 0; chunk < 4; chunk++)
                    {
                        uint size = reader.ReadUInt32();
                        reader.Read(header, 0, KeyLength);

                        // Skip size field in output
                        writer.Seek(4, SeekOrigin.Current);

                        int mapIndex = (int)(size >> chunk + 5) & 0x0F;
                        int keyIndex = (int)(size >> chunk + 1) & 0x0F;

                        writer.Write(Decrypt(header, mapIndex, keyIndex));

                        // Skip remaining data
                        long remaining = size - KeyLength + sizeof(uint);
                        if (remaining > 0)
                        {
                            reader.BaseStream.Seek(remaining, SeekOrigin.Current);
                            writer.BaseStream.Seek(remaining, SeekOrigin.Current);
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] Decrypt(byte[] buffer, int mapIndex, int keyIndex)
        {
            // Get the correct map and key segments
            ReadOnlySpan<byte> map = new ReadOnlySpan<byte>(ObfuscationMapsData, MapOffsets[mapIndex], MapSize);
            ReadOnlySpan<byte> key = new ReadOnlySpan<byte>(EncryptionKeysData, KeyOffsets[keyIndex], KeyLength);

            Span<byte> temp = stackalloc byte[MapSize];

            // First permutation using obfuscation maps
            for (int i = 0; i < MapSize; i++)
            {
                temp[map[i]] = buffer[i];
            }

            // XOR with key
            for (int i = 0; i < MapSize; i++)
            {
                buffer[i] = (byte)(temp[i] ^ key[i]);
            }

            // XOR remaining bytes
            for (int i = MapSize; i < KeyLength; i++)
            {
                buffer[i] ^= key[i];
            }

            return buffer;
        }
    }
}