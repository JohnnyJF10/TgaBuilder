using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.IO;
using System.Net.Security;
using System.Text;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.Level
{
    public partial class TenLevel
    {
        internal enum TenTextureType
        {
            RoomTexture,
            ObjectTexture,
            StaticTexture,
            AnimatedTexture,
            SpriteTexture,
        }

        internal void ReadLevel(string fileName)
        {
            using var reader = new BinaryReader(File.OpenRead(fileName));

            string tenMark = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (tenMark != "TEN\0")
                throw new NotSupportedException("Specified file is an unknown TEN file format. TEN header not found");

            versionMajor = reader.ReadByte();
            versionMinor = reader.ReadByte();
            versionBuild = reader.ReadByte();
            versionRevision = reader.ReadByte();


            int systemHash = reader.ReadInt32();
            int levelHash = reader.ReadInt32();

            if (versionMajor < 2 && versionMinor < 6)
                throw new NotSupportedException($"TEN files of version lower than 2.6 are not supported. File version: {versionMajor}.{versionMinor}.{versionBuild}.{versionRevision}");

            if (versionMajor < 2 && versionMinor < 7)
                ReadTenData_pre_1_7(reader);
            else
                ReadTenData(reader);
        }

        private void ReadTenData(BinaryReader reader)
        {
            uint mediaUncompressedSize = reader.ReadUInt32();
            uint mediaCompressedSize = reader.ReadUInt32();

            using (var mediaStream = DecompressTenStream(reader.BaseStream, mediaCompressedSize))
            using (var mediaReader = new BinaryReader(mediaStream))
            {
                ReadTextures(mediaReader);
                ReadSamples(mediaReader);
            }

            uint geometryUncompressedSize = reader.ReadUInt32();
            uint geometryCompressedSize = reader.ReadUInt32();

            using (var geometryStream = DecompressTenStream(reader.BaseStream, geometryCompressedSize))
            using (var geometryReader = new BinaryReader(geometryStream))
            {
                ReadStaticRoomData(geometryReader);
            }
        }

        private void ReadTenData_pre_1_7(BinaryReader reader)
        {
            uint dataCompressedSize = reader.ReadUInt32();

            using (var dataStream = DecompressTenStream(reader.BaseStream, dataCompressedSize))
            using (var dataReader = new BinaryReader(dataStream))
            {
                ReadTextures(dataReader);
                ReadStaticRoomData(dataReader, true);
            }
        }

        private void ReadTextures(BinaryReader levelReader)
        {
            int numRoomTextures = levelReader.ReadInt32();
            int size;

            int width;
            int height;

            bool hasNormalMap;

            for (int i = 0; i < numRoomTextures; i++)
            {
                width = levelReader.ReadInt32();
                height = levelReader.ReadInt32();
                size = levelReader.ReadInt32();
                var textureData = levelReader.ReadBytes(size);

                TexDimsList.Add((width, height, size)); // Placeholder for x, y, size
                TexPagesList.Add(GetRentedPixelArrayFromPng(textureData, width, height));

                hasNormalMap = levelReader.ReadByte() == 1;
                if (hasNormalMap)
                {
                    size = levelReader.ReadInt32();
                    levelReader.ReadBytes(size); //Normal map data
                }
            }

            int numObjectTextures = levelReader.ReadInt32();
            for (int i = 0; i < numObjectTextures; i++)
            {

                width = levelReader.ReadInt32();
                height = levelReader.ReadInt32();

                size = levelReader.ReadInt32();
                levelReader.ReadBytes(size); //Object texture data

                TexDimsList.Add((0, 0, 0));
                TexPagesList.Add(Array.Empty<byte>());

                hasNormalMap = levelReader.ReadByte() == 1;
                if (hasNormalMap)
                {
                    size = levelReader.ReadInt32();
                    levelReader.ReadBytes(size); //Normal map data
                }

            }
            int numStaticsTextures = levelReader.ReadInt32();
            for (int i = 0; i < numStaticsTextures; i++)
            {
                width = levelReader.ReadInt32();
                height = levelReader.ReadInt32();
                size = levelReader.ReadInt32();

                levelReader.ReadBytes(size); //Static texture data

                TexDimsList.Add((0, 0, 0));
                TexPagesList.Add(Array.Empty<byte>());

                hasNormalMap = levelReader.ReadByte() == 1;

                if (hasNormalMap)
                {
                    size = levelReader.ReadInt32();

                    levelReader.ReadBytes(size); //Normal map data
                }
            }

            int numAnimatedTextures = levelReader.ReadInt32();
            for (int i = 0; i < numAnimatedTextures; i++)
            {
                width = levelReader.ReadInt32();
                height = levelReader.ReadInt32();
                size = levelReader.ReadInt32();
                var animatedMapData = levelReader.ReadBytes(size);

                TexDimsList.Add((width, height, size));
                TexPagesList.Add(GetRentedPixelArrayFromPng(animatedMapData, width, height));
                hasNormalMap = levelReader.ReadByte() == 1;
                if (hasNormalMap)
                {
                    size = levelReader.ReadInt32();
                    levelReader.ReadBytes(size); //Normal map data
                }
            }


            int numSpriteTextures = levelReader.ReadInt32();
            for (int i = 0; i < numSpriteTextures; i++)
            {
                width = levelReader.ReadInt32();
                height = levelReader.ReadInt32();
                size = levelReader.ReadInt32();

                levelReader.ReadBytes(size); //Sprite texture data
            }

            //Sky texture
            width = levelReader.ReadInt32();
            height = levelReader.ReadInt32();
            size = levelReader.ReadInt32();
            TexDimsList.Add((0, 0, 0));
            TexPagesList.Add(Array.Empty<byte>());
            levelReader.ReadBytes(size); //Sky texture data
        }

        private void ReadSamples(BinaryReader levelReader)
        {

            int ReadCount(int max = int.MaxValue)
            {
                int count = levelReader.ReadInt32();
                if (count < 0 || count > max)
                    throw new InvalidDataException($"Invalid Count: {count}");
                return count;
            }


            short soundMapSize = levelReader.ReadInt16();
            int soundMapByteSize = soundMapSize * sizeof(short);
            _ = levelReader.ReadBytes(soundMapByteSize); // SoundMap

            int sampleInfoCount = ReadCount();
            if (sampleInfoCount == 0)
                return;

            int sampleInfoByteSize = sampleInfoCount * 8; // 
            _ = levelReader.ReadBytes(sampleInfoByteSize); // SoundDetails

            int sampleCount = ReadCount();
            if (sampleCount <= 0)
                return;

            for (int i = 0; i < sampleCount; i++)
            {
                int uncompressedSize = levelReader.ReadInt32();
                int compressedSize = levelReader.ReadInt32();

                _ = levelReader.ReadBytes(compressedSize); 
                                             
            }
        }

        void ReadStaticRoomData(BinaryReader levelReader, bool Pre_1_7 = false)
        {
            int[] texCorners = new int[8];
            bool toSkip = false;
            int pageWidth = 0;
            int pageHeight = 0;


            int roomCount = levelReader.ReadInt32();

            for (int i = 0; i < roomCount; i++)
            {
                if (Pre_1_7)
                {
                    var name = ReadString(levelReader); // room.Name
                    var tagCount = levelReader.ReadInt32(); // room.tagCount
                    for (int j = 0; j < tagCount; j++)
                    {
                        var tagName = ReadString(levelReader); // room.Tags[j].Name
                    }
                }

                levelReader.ReadInt32(); // room.Position.x
                                         // room.Position.y = 0
                levelReader.ReadInt32(); // room.Position.z
                levelReader.ReadInt32(); // room.BottomHeight
                levelReader.ReadInt32(); // room.TopHeight

                int vertexCount = levelReader.ReadInt32();

               levelReader.ReadBytes(vertexCount * 12); // positions
               levelReader.ReadBytes(vertexCount * 12); // colors
               levelReader.ReadBytes(vertexCount * 12); // effects

                int bucketCount = levelReader.ReadInt32();
                for (int j = 0; j < bucketCount; j++)
                {
                    toSkip = false;
                    int pageIndex = levelReader.ReadInt32(); // page

                    if (pageIndex >= TexDimsList.Count)
                        toSkip = true;
                    else
                    {
                        pageWidth = TexDimsList[pageIndex].width;
                        pageHeight = TexDimsList[pageIndex].height;
                    }

                    levelReader.ReadByte();  // blendMode
                    levelReader.ReadByte();  // animated

                    int polyCount = levelReader.ReadInt32();
                    for (int k = 0; k < polyCount; k++)
                    {
                        int shape = levelReader.ReadInt32();
                        levelReader.ReadInt32(); // animatedSequence
                        levelReader.ReadInt32(); // animatedFrame

                        int count = (shape == 0) ? 4 : 3;
                        for (int l = 0; l < count; l++) _ = levelReader.ReadInt32();        // indices
                        for (int l = 0; l < count; l++)
                        {
                            texCorners[l * 2] = (int)Math.Round(levelReader.ReadSingle() * pageWidth);   // textureCorners.x
                            texCorners[l * 2 + 1] = (int)Math.Round(levelReader.ReadSingle() * pageHeight); // textureCorners.y
                        }       

                        levelReader.ReadBytes(count * 12 * 3); // 3D Info

                        if (toSkip) continue;

                        var boundingBox = shape == 0 ? GetBoundingBox4(texCorners) : GetBoundingBox3(texCorners);

                        if (boundingBox.width <= 4) continue;
                        if (boundingBox.height <= 4) continue;
                        if (!IsPowerOfTwo(boundingBox.width)) continue;
                        if (!IsPowerOfTwo(boundingBox.height)) continue;
                        if (boundingBox.width >= MAX_SUPPORTED_TEX_SIZE) continue;
                        if (boundingBox.height >= MAX_SUPPORTED_TEX_SIZE) continue;

                        RoomsTextureInfos.Add((pageIndex, boundingBox.x, boundingBox.y, boundingBox.width, boundingBox.height));
                    }
                }

                // Read portal data
                int portalCount = levelReader.ReadInt32();
                levelReader.ReadBytes(62 * portalCount); 

                // Read floor data
                var zSize = levelReader.ReadInt32(); // room.ZSize
                var xSize = levelReader.ReadInt32(); // room.XSize
                levelReader.ReadBytes(100 * zSize * xSize);

                if (Pre_1_7)
                {
                    // Read room ambient
                    levelReader.ReadBytes(12); 
                }


                // Read light data
                int lightCount = levelReader.ReadInt32();
                levelReader.ReadBytes(58 * lightCount); 

                if (Pre_1_7)
                {
                    // Read room statics
                    int staticCount = levelReader.ReadInt32();
                    for (int j = 0; j < staticCount; j++)
                    {
                        levelReader.ReadBytes(44);
                        var staticName = ReadString(levelReader); // roomStatics[j].Name
                    }

                    // Read Trigger Volume
                    int triggerVolumeCount = levelReader.ReadInt32();
                    for (int j = 0; j < triggerVolumeCount; j++)
                    {
                        levelReader.ReadBytes(44); // Trigger Volume data
                        var triggerVolumeName = ReadString(levelReader); // roomTriggerVolumes[j].Name
                        levelReader.ReadInt32(); // EventSetIndex 
                    }

                    var flippedRoom = levelReader.ReadInt32(); // room.flippedRoom
                    var flags = levelReader.ReadInt32(); // room.flags
                    var meshEffect = levelReader.ReadInt32(); // room.meshEffect
                    var reverbType = levelReader.ReadInt32(); // room.reverbType
                    var flipNumber = levelReader.ReadInt32(); // room.flipNumber
                }
            }
        }

        private Stream DecompressTenStream(Stream baseStream, uint compressedSize)
        {
            var limitedStream = new SubStream(baseStream, compressedSize);
            return new InflaterInputStream(limitedStream);
        }

        public class SubStream : Stream
        {
            private readonly Stream _base;
            private readonly long _start;
            private readonly long _length;
            private long _position;

            public SubStream(Stream baseStream, uint length)
            {
                _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
                _start = baseStream.Position;
                _length = length;
                _position = 0;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_position >= _length) return 0;

                long remaining = _length - _position;
                if (count > remaining)
                    count = (int)remaining;

                int bytesRead = _base.Read(buffer, offset, count);
                _position += bytesRead;
                return bytesRead;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _length;
            public override long Position
            {
                get => _position;
                set => throw new NotSupportedException();
            }

            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
