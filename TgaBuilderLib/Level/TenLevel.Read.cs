using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Text;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.Level
{
    public partial class TenLevel
    {
        public enum TenVersion
        {
            Unknown,
            Version_1_5,
            Version_1_6,
            Version_1_7,
        }

        protected override void ReadLevel(string fileName)
        {
            using var reader = new BinaryReader(File.OpenRead(fileName));

            string tenMark = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (tenMark != "TEN\0")
                throw new NotSupportedException("Specified file is an unknown TEN file format. TEN header not found");

            int versionMajor = reader.ReadByte();
            int versionMinor = reader.ReadByte();
            int versionBuild = reader.ReadByte();
            int versionRevision = reader.ReadByte();

            Version = versionMinor switch
            {
                5       => TenVersion.Version_1_5,
                6       => TenVersion.Version_1_6,
                >= 7    => TenVersion.Version_1_7,
                _       => TenVersion.Unknown
            };

            int systemHash = reader.ReadInt32();
            int levelHash = reader.ReadInt32();

            if (Version == TenVersion.Unknown)
                throw new NotSupportedException($"Unsupported TEN version. " +
                    $"File version: {versionMajor}.{versionMinor}.{versionBuild}.{versionRevision}");

            if (Version < TenVersion.Version_1_7)
                ReadTenData_pre_1_7(reader);
            else
                ReadTenData(reader);
        }

        private void ReadTenData(BinaryReader reader)
        {
            uint mediaUncompressedSize = reader.ReadUInt32();
            uint mediaCompressedSize = reader.ReadUInt32();

            using (var mediaStream = DecompressStream(reader.BaseStream, mediaCompressedSize))
            using (var mediaReader = new BinaryReader(mediaStream))
            {
                ReadTextures(mediaReader);
                ReadSamples(mediaReader);
            }

            uint geometryUncompressedSize = reader.ReadUInt32();
            uint geometryCompressedSize = reader.ReadUInt32();

            using (var geometryStream = DecompressStream(reader.BaseStream, geometryCompressedSize))
            using (var geometryReader = new BinaryReader(geometryStream))
            {
                ReadStaticRoomData(geometryReader);
            }
        }

        private void ReadTenData_pre_1_7(BinaryReader reader)
        {
            uint dataCompressedSize = reader.ReadUInt32();

            using (var dataStream = DecompressStream(reader.BaseStream, dataCompressedSize))
            using (var dataReader = new BinaryReader(dataStream))
            {
                ReadTextures(dataReader);
                ReadStaticRoomData(dataReader);
            }
        }

        private void ReadTextures(BinaryReader levelReader)
        {
            ReadTexturePages(levelReader);                                          // Read room textures
            ReadTexturePages(levelReader, discardTextures: true);                   // Read object textures
            ReadTexturePages(levelReader, discardTextures: true);                   // Read static textures
            ReadTexturePages(levelReader);                                          // Read animated textures
            ReadTexturePages(levelReader, discardTextures: true, isSprites: true);  // Read sprite textures
            ReadTexturePages(levelReader, discardTextures: true, isSky: true);      // Read sky texture
        }

        private void ReadTexturePages(BinaryReader levelReader, bool discardTextures = false, bool isSprites = false, bool isSky = false)
        {
            int size, width, height, bytesRead;

            bool hasNormalMap;

            int numRoomTextures = isSky ? 1 : levelReader.ReadInt32();
            for (int i = 0; i < numRoomTextures; i++)
            {
                width = levelReader.ReadInt32();
                height = levelReader.ReadInt32();
                size = levelReader.ReadInt32();

                if (discardTextures)
                {
                    levelReader.ReadBytes(size); 
                    _texDimsList.Add((0, 0, 0));
                    _texPagesList.Add(Array.Empty<byte>());
                }
                else
                {
                    var textureData = _bytePool.Rent(size);

                    try
                    {
                        bytesRead = levelReader.Read(textureData, 0, size);

                        if (bytesRead != size)
                            throw new InvalidOperationException(
                                $"Expected to read {size} bytes, but only read {bytesRead} bytes for texture {i}.");

                        _texDimsList.Add((width, height, size));
                        _texPagesList.Add(GetRentedPixelArrayFromPng(textureData, width, height, size));
                    }
                    finally
                    {
                        _bytePool.Return(textureData);
                    }
                }

                if (isSprites || isSky)
                    continue;

                hasNormalMap = levelReader.ReadByte() == 1;
                if (hasNormalMap)
                {
                    size = levelReader.ReadInt32();
                    levelReader.ReadBytes(size); //Normal map data
                }
            }
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

        void ReadStaticRoomData(BinaryReader levelReader)
        {
            int[] texCorners = new int[8];
            bool toSkip = false;
            int pageWidth = 0;
            int pageHeight = 0;
            string stringValue;


            int roomCount = levelReader.ReadInt32();

            for (int i = 0; i < roomCount; i++)
            {
                if (Version == TenVersion.Version_1_6)
                {
                    stringValue = ReadString(levelReader); // room.Name
                    var tagCount = levelReader.ReadInt32(); // room.tagCount
                    for (int j = 0; j < tagCount; j++)
                    {
                        stringValue = ReadString(levelReader); // room.Tags[j].Name
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

                    if (pageIndex >= _texDimsList.Count)
                        toSkip = true;
                    else
                    {
                        pageWidth = _texDimsList[pageIndex].width;
                        pageHeight = _texDimsList[pageIndex].height;
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

                        _roomsTextureInfos.Add((pageIndex, boundingBox.x, boundingBox.y, boundingBox.width, boundingBox.height));
                    }
                }

                // Read portal data
                int portalCount = levelReader.ReadInt32();
                levelReader.ReadBytes(62 * portalCount); 

                // Read floor data
                var zSize = levelReader.ReadInt32(); // room.ZSize
                var xSize = levelReader.ReadInt32(); // room.XSize
                levelReader.ReadBytes(100 * zSize * xSize);

                if (Version < TenVersion.Version_1_7)
                {
                    // Read room ambient
                    levelReader.ReadBytes(12); 
                }


                // Read light data
                int lightCount = levelReader.ReadInt32();
                levelReader.ReadBytes(58 * lightCount);

                if (Version > TenVersion.Version_1_6)
                    continue;


                // Read room statics
                int staticCount = levelReader.ReadInt32();
                for (int j = 0; j < staticCount; j++)
                {
                    // Static data
                    if (Version == TenVersion.Version_1_6)
                        levelReader.ReadBytes(44);
                    else if (Version == TenVersion.Version_1_5)
                        levelReader.ReadBytes(36);

                    stringValue = ReadString(levelReader); // roomStatics[j].Name
                }

                // Read Trigger Volume
                int triggerVolumeCount = levelReader.ReadInt32();
                for (int j = 0; j < triggerVolumeCount; j++)
                {
                    if (Version == TenVersion.Version_1_5)
                    {
                        levelReader.ReadBytes(48); // Trigger Volume data
                    }
                    else if (Version == TenVersion.Version_1_6)
                    {
                        levelReader.ReadBytes(44); // Trigger Volume data
                        stringValue = ReadString(levelReader); // roomTriggerVolumes[j].Name
                        levelReader.ReadInt32(); // EventSetIndex 
                    }
                }

                var flippedRoom = levelReader.ReadInt32(); // room.flippedRoom
                var flags = levelReader.ReadInt32(); // room.flags
                var meshEffect = levelReader.ReadInt32(); // room.meshEffect
                var reverbType = levelReader.ReadInt32(); // room.reverbType
                var flipNumber = levelReader.ReadInt32(); // room.flipNumber

            }
        }

        private long ReadLEB128(BinaryReader reader, bool signedValue)
        {
            long result = 0;
            int shift = 0;
            byte currentByte;

            do
            {
                currentByte = reader.ReadByte();
                result |= (long)(currentByte & 0x7F) << shift;
                shift += 7;
            }
            while ((currentByte & 0x80) != 0);

            if (signedValue && shift < 64 && (currentByte & 0x40) != 0)
            {
                result |= -1L << shift;
            }

            return result;
        }

        private string ReadString(BinaryReader reader)
        {
            long numBytes = ReadLEB128(reader, signedValue: false);

            if (numBytes <= 0)
                return string.Empty;

            byte[] stringBytes = reader.ReadBytes((int)numBytes);

            return System.Text.Encoding.UTF8.GetString(stringBytes);
        }
    }
}
