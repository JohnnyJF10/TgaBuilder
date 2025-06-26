using System.IO;
using System.Text;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.Ten
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

            string levelName = fileName;

            byte versionMajor ;
            byte versionMinor ;
            byte versionBuild ;
            byte versionRevision;

            byte[] mediaData;
            byte[] geometryData;

            //using (var writer = new BinaryWriter(File.OpenWrite(fileName + "Uncropmressed.ten")))
            using (var reader = new BinaryReader(File.OpenRead(fileName)))
            {
                string tenMark = ASCIIEncoding.ASCII.GetString(reader.ReadBytes(4));

                if (String.Compare(tenMark, "TEN\0") != 0)
                    throw new NotSupportedException(
                        $"Specified file is an unknown TEN file format. TEN header not found");

                versionMajor = reader.ReadByte();
                versionMinor = reader.ReadByte();
                versionBuild = reader.ReadByte();
                versionRevision = reader.ReadByte();

                if (versionMajor < 2 && versionMinor < 7)
                    throw new NotSupportedException(
                        $"TEN versions earlier than 2.8 are not supported. " +
                        $"Version found: {versionMajor}.{versionMinor}.{versionBuild}.{versionRevision}");

                int systemHash = reader.ReadInt32();
                int levelHash =  reader.ReadInt32();

               

                var mediaUncompressedSize = reader.ReadUInt32(); 
                var mediaCompressedSize = reader.ReadUInt32();
                mediaData = DecompressTen(reader, mediaCompressedSize);
                

                var geometryUncompressedSize = reader.ReadUInt32(); 
                var geometryCompressedSize = reader.ReadUInt32();
                geometryData = DecompressTen(reader, geometryCompressedSize);
            }

            // Media data
            using (var stream = new MemoryStream(mediaData))
            using (var levelReader = new BinaryReader(stream))
            {
                ReadTextures(levelReader);
                ReadSamples(levelReader);
            }

            // Geometry data
            using (var stream = new MemoryStream(geometryData))
            using (var levelReader = new BinaryReader(stream))
            {
                ReadStaticRoomData(levelReader);
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

        void ReadStaticRoomData(BinaryReader levelReader)
        {
            int[] texCorners = new int[8];
            bool toSkip = false;
            int pageWidth = 0;
            int pageHeight = 0;

            int ReadCount(int max = int.MaxValue)
            {
                int count = levelReader.ReadInt32();
                if (count < 0 || count > max)
                    throw new InvalidDataException($"Invalid Count: {count}");
                return count;
            }

            int roomCount = ReadCount();

            for (int i = 0; i < roomCount; i++)
            {
                _ = levelReader.ReadInt32(); // room.Position.x
                _ = 0;            // room.Position.y
                _ = levelReader.ReadInt32(); // room.Position.z
                _ = levelReader.ReadInt32(); // room.BottomHeight
                _ = levelReader.ReadInt32(); // room.TopHeight

                int vertexCount = ReadCount(1024 * 1024 * 1024);

                _ = levelReader.ReadBytes(vertexCount * 12); // positions
                _ = levelReader.ReadBytes(vertexCount * 12); // colors
                _ = levelReader.ReadBytes(vertexCount * 12); // effects

                int bucketCount = ReadCount();
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

                    _ = levelReader.ReadByte();  // blendMode
                    _ = levelReader.ReadByte();  // animated

                    int polyCount = ReadCount(1024 * 1024 * 1024);
                    for (int k = 0; k < polyCount; k++)
                    {
                        int shape = levelReader.ReadInt32();
                        _ = levelReader.ReadInt32(); // animatedSequence
                        _ = levelReader.ReadInt32(); // animatedFrame

                        int count = (shape == 0) ? 4 : 3;
                        for (int l = 0; l < count; l++) _ = levelReader.ReadInt32();        // indices
                        for (int l = 0; l < count; l++)
                        {
                            texCorners[l * 2] = (int)Math.Round(levelReader.ReadSingle() * pageWidth);   // textureCorners.x
                            texCorners[l * 2 + 1] = (int)Math.Round(levelReader.ReadSingle() * pageHeight); // textureCorners.y
                        }       

                        _ = levelReader.ReadBytes(count * 12 * 3); // 3D Info

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
                int portalCount = ReadCount();
                _ = levelReader.ReadBytes(62 * portalCount); 

                // Read floor data
                var zSize = levelReader.ReadInt32(); // room.ZSize
                var xSize = levelReader.ReadInt32(); // room.XSize
                _ = levelReader.ReadBytes(100 * zSize * xSize);

                // Read light data
                int lightCount = ReadCount();
                _ = levelReader.ReadBytes(58 * lightCount); 
            }
        }

        private byte[] DecompressTen(BinaryReader reader, uint compressedSize)
        {
            byte[] levelData = reader.ReadBytes((int)compressedSize);
            levelData = ZLib.DecompressData(levelData);
            return levelData;
        }
    }
}
