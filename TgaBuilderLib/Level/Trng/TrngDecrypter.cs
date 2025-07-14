using System.IO;

namespace TgaBuilderLib.Level
{
    public static partial class TrngDecrypter
    {
        private const int KeyLength = 99;
        private const int MapCount = 16;
        private const int MapSize = 50;


        // Key offsets - precomputed offsets for each key
        private static readonly int[] KeyOffsets = new int[MapCount];

        // Map offsets - precomputed offsets for each map
        private static readonly int[] MapOffsets = new int[MapCount];

        static TrngDecrypter()
        {
            // Initialize offsets
            for (int i = 0; i < MapCount; i++)
            {
                KeyOffsets[i] = i * KeyLength;
                MapOffsets[i] = i * MapSize;
            }
        }

        public static bool DecryptLevel(string source, string target)
        {
            if (!File.Exists(source)) return false;

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
                        byte[] header = reader.ReadBytes(KeyLength);

                        // Skip size field in output
                        writer.Seek(4, SeekOrigin.Current);

                        int mapIndex = (int)(size >> chunk + 5) & 0x0F;
                        int keyIndex = (int)(size >> chunk + 1) & 0x0F;

                        writer.Write(DecryptOptimized(header, mapIndex, keyIndex));

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

        private static byte[] DecryptOptimized(byte[] buffer, int mapIndex, int keyIndex)
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