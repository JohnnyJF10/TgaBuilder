using System.IO;
using THelperLib.Abstraction;
using THelperLib.Trng;
using THelperLib.Utils;

namespace THelperLib.Tr
{
    public partial class TrLevel
    {
        internal void ReadLevel(string fileName)
		{

            long uncompressedEndPos = 0;
			bool needsDecrypt = false;
			string levelName = fileName;

            byte[] levelData;

            using (var reader = new BinaryReader(File.OpenRead(fileName)))
			{
                uint versionUInt = reader.ReadUInt32();
                Version = versionUInt switch
                {
                    0x00000020 => TrVersion.TR1,
                    0x0000002D => TrVersion.TR2,
                    0xFF180038 => TrVersion.TR3,
                    0xFF080038 => TrVersion.TR3,
                    0xFF180034 => TrVersion.TR3,
                    0x00345254 => TrVersion.TR4,
                    0x63345254 => TrVersion.TR4,
                    _ => throw new FileFormatException("Unknown game version 0x" + versionUInt.ToString("X") + ".")
                };

				if (versionUInt == 0x63345254) 
					needsDecrypt = true;
			}

			// If encrypted TRNG level, make temporary copy of it and decrypt it.
			// Copy will be deleted after parsing.
			if (needsDecrypt)
                levelName = NgGetDecryptedLevelName(fileName);

            using (var reader = new BinaryReader(File.OpenRead(levelName)))
            {
                string Name = Path.GetFileNameWithoutExtension(levelName);

                reader.ReadUInt32();

                if (Version == TrVersion.TR4 && fileName.ToLower().Trim().EndsWith(".trc"))
                    Version = TrVersion.TRC;

                // Check for NG header
                IsNg = false;
                if (Version == TrVersion.TR4)
                    CheckForNgHeader(reader);

                // Read the palette24 only for TR2 and TR3, TR1 has the palette24 near the end of the file
                if (Version == TrVersion.TR2 || Version == TrVersion.TR3)
                {
                    ReadPaletteTr2Tr3(reader);
                }

                // Read 8 bit and 16 bit textures if Version is <= TR3
                if (Version == TrVersion.TR1 || Version == TrVersion.TR2 || Version == TrVersion.TR3)
                {
                    Read8BitAnd16BitAtlas(Version, reader);
                }

                uncompressedEndPos = reader.BaseStream.Position;

                // Read 16 and 32 bit textures and uncompress them if TR4 and TRC
                if (Version == TrVersion.TR4 || Version == TrVersion.TRC)
                {
                    ReadAtlasTr4Tr5(reader);
                }

                // Put the level geometry into a byte array
                if (Version <= TrVersion.TR3)
                    levelData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                else if (Version == TrVersion.TR4)
                {
                    var uncompressedSize = reader.ReadUInt32();
                    var compressedSize = reader.ReadUInt32();
                    levelData = DecompressTr4(reader, compressedSize);
                }
                else // if (Version == TrVersion.TRC)
                {
                    reader.ReadBytes(32);
                    var uncompressedSize = reader.ReadUInt32();
                    var compressedSize = reader.ReadUInt32();
                    levelData = reader.ReadBytes((int)compressedSize);
                }
            }

            // Follow up stream with the level data
            using (var stream = new MemoryStream(levelData))
			using (var levelReader = new BinaryReader(stream))
            {
                levelReader.ReadUInt32();
                // Read rooms
                ReadRooms(Version, levelReader);

                // Floordata
                var numFloorData = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numFloorData * 2);

                // Mesh data
                var numMeshData = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numMeshData * 2);

                var numMeshPointers = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numMeshPointers * 4);

                // Animations
                var numAnimations = levelReader.ReadUInt32();
                if (Version <= TrVersion.TR3)
                    levelReader.ReadBytes((int)numAnimations * 32);
                else
                    levelReader.ReadBytes((int)numAnimations * 40);

                // State changes
                var numStateChanges = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numStateChanges * 6);

                // Anim dispatches
                var numDispatches = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numDispatches * 8);


                // Anim commands
                var numAnimCommands = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numAnimCommands * 2);


                // Mesh trees
                var numMeshTrees = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numMeshTrees * 4);

                // Keyframes
                var numFrames = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numFrames * 2);

                // Moveables
                var numMoveables = levelReader.ReadUInt32();
                if (Version == TrVersion.TRC)
                    levelReader.ReadBytes((int)numMoveables * 20);
                else
                    levelReader.ReadBytes((int)numMoveables * 18);

                // Static meshes
                var numStaticMeshes = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numStaticMeshes * 32);

                // If Version <= TR2 object textures are here
                // Anim Infos have not been read yet, so we need to
                // store the reader position and come here again later
                if (Version == TrVersion.TR1 || Version == TrVersion.TR2)
                {
                    textureInfosStreamPosition = levelReader.BaseStream.Position;

                    var numObjectTextures = levelReader.ReadUInt32();
                    levelReader.ReadBytes((int)numObjectTextures * 20);
                }

                // SPR marker
                var marker = "";
                if (Version == TrVersion.TR4)
                    marker = System.Text.Encoding.ASCII.GetString(levelReader.ReadBytes(3));
                if (Version == TrVersion.TRC)
                    marker = System.Text.Encoding.ASCII.GetString(levelReader.ReadBytes(4));

                // Sprite textures
                var numSpriteTextures = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numSpriteTextures * 16);

                // Sprite sequences
                var numSpriteSequences = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numSpriteSequences * 8);

                // Cameras
                var numCameras = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numCameras * 16);

                // Flyby cameras
                if (Version == TrVersion.TR4 || Version == TrVersion.TRC)
                {
                    var numFlybyCameras = levelReader.ReadUInt32();
                    levelReader.ReadBytes((int)numFlybyCameras * 40);
                }

                // Sound sources
                var numSoundSources = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numSoundSources * 16);

                // Boxes
                var numBoxes = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numBoxes * (Version == TrVersion.TR1 ? 20 : 8));

                // Overlaps
                var numOverlaps = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numOverlaps * 2);

                // Zones
                levelReader.ReadBytes((int)numBoxes * (Version == TrVersion.TR1 ? 12 : 20));

                // Animated textures
                ReadAnimatedTextures(levelReader);

                // If Version >= TR3, object textures are here, amis already red, so afterwards we are finished
                if (Version == TrVersion.TR3 || Version == TrVersion.TR4 || Version == TrVersion.TRC)
                {
                    if (Version == TrVersion.TR4)
                        marker = System.Text.Encoding.ASCII.GetString(levelReader.ReadBytes(4));
                    if (Version == TrVersion.TRC)
                        marker = System.Text.Encoding.ASCII.GetString(levelReader.ReadBytes(5));

                    ReadTextureInfos(levelReader);

                    if (needsDecrypt)
                        NgRemoveTempFile(levelName);

                    return; // Finished reading level for TR3, TR4 and TRC
                }

                // Items
                var numEntities = levelReader.ReadUInt32();
                levelReader.ReadBytes((int)numEntities * (Version == TrVersion.TR1 ? 22 : 24));

                if (Version == TrVersion.TR1 || Version == TrVersion.TR2)
                {
                    // Lightmap
                    levelReader.ReadBytes(8192);

                    // Palette
                    if (Version == TrVersion.TR1)
                    {
                        ReadPaletteTr1(levelReader);
                    }
                }

                // If Version is TR1 or TR2, we need to read the object textures
                // as the animated textures infos are available now
                levelReader.BaseStream.Position = textureInfosStreamPosition;
                ReadTextureInfos(levelReader);
            }
        }

        private byte[] DecompressTr4(BinaryReader reader, uint compressedSize)
        {
            byte[] levelData = reader.ReadBytes((int)compressedSize);
            levelData = ZLib.DecompressData(levelData);
            return levelData;
        }

        // --- Rooms Reading ---

        private void ReadRooms(TrVersion Version, BinaryReader levelReader)
        {
            var numRooms = Version != TrVersion.TRC ? levelReader.ReadUInt16() : levelReader.ReadUInt32();
            for (var i = 0; i < numRooms; i++)
            {
                if (Version != TrVersion.TRC)
                {
                    // Room info
                    levelReader.ReadBytes(16);

                    ReadRoomData(levelReader);

                    var numPortals = levelReader.ReadUInt16();
                    levelReader.ReadBytes(numPortals * 32);

                    var numXsectors = levelReader.ReadUInt16();
                    var numZsectors = levelReader.ReadUInt16();
                    levelReader.ReadBytes(numXsectors * numZsectors * 8);

                    // Ambient intensity 1 & 2
                    levelReader.ReadUInt16();
                    if (Version != TrVersion.TR1)
                        levelReader.ReadUInt16();

                    // Lightmode
                    if (Version == TrVersion.TR2)
                        levelReader.ReadUInt16();

                    var numLights = levelReader.ReadUInt16();
                    if (Version == TrVersion.TR1)
                        levelReader.ReadBytes(numLights * 18);
                    if (Version == TrVersion.TR2)
                        levelReader.ReadBytes(numLights * 24);
                    if (Version == TrVersion.TR3)
                        levelReader.ReadBytes(numLights * 24);
                    if (Version == TrVersion.TR4)
                        levelReader.ReadBytes(numLights * 46);

                    var numRoomStaticMeshes = levelReader.ReadUInt16();
                    if (Version == TrVersion.TR1)
                        levelReader.ReadBytes(numRoomStaticMeshes * 18);
                    else
                        levelReader.ReadBytes(numRoomStaticMeshes * 20);

                    // Various flags and alternate room
                    if (Version <= TrVersion.TR2)
                        levelReader.ReadBytes(4);
                    else
                        levelReader.ReadBytes(7);
                }
                else
                {
                    var xela = System.Text.Encoding.ASCII.GetString(levelReader.ReadBytes(4));
                    var roomDataSize = levelReader.ReadUInt32();
                    levelReader.ReadBytes((int)roomDataSize);
                }
            }
        }

        private void ReadRoomData(BinaryReader levelReader)
        {
            var numDataWords = levelReader.ReadUInt32();

            var numVertices = levelReader.ReadUInt16();
            if (Version == TrVersion.TR1)
                levelReader.ReadBytes(numVertices * 8);
            else
                levelReader.ReadBytes(numVertices * 12);


            var numRectangles = levelReader.ReadUInt16();
            for (var i = 0; i < numRectangles; i++)
            {
                levelReader.ReadBytes(8); // Vertices
                RectTexIndices.Add(levelReader.ReadUInt16()); // Texture
            }

            var numTriangles = levelReader.ReadUInt16();
            for (var i = 0; i < numTriangles; i++)
            {
                levelReader.ReadBytes(6); // Vertices
                TriagTexIndices.Add(levelReader.ReadUInt16()); // Texture
            }

            var numSprites = levelReader.ReadUInt16();
            levelReader.ReadBytes(numSprites * 4);
        }

        // --- Texture Infos Reading ---

        private void ReadTextureInfos(BinaryReader levelReader)
        {
            var ufixes = new ushort[8];
            int mappingCorrection = 0;

            var numObjectTextures = levelReader.ReadUInt32();
            for (var i = 0; i < numObjectTextures; i++)
            {
                levelReader.ReadUInt16(); // Attributes
                ushort PageAndFlags = levelReader.ReadUInt16(); 

                if (Version >= TrVersion.TR4)
                    mappingCorrection = levelReader.ReadUInt16() & 0xF; 

                for (int j = 0; j < 8; j++)
                    ufixes[j] = levelReader.ReadUInt16();

                if (Version == TrVersion.TR4)
                    levelReader.ReadBytes(16);
                if (Version == TrVersion.TRC)
                    levelReader.ReadBytes(18);

                if (AnimTexIndices.Contains((ushort)i))
                {
                    bool TeUsed = (ufixes[0] & 0xff) == 0x7f;

                    var (x, y, width, height) = GetBoundingBox4(ufixes);

                    if (width <= 1 || height <= 1)
                        continue;

                    // TE Corrections
                    if (Version >= TrVersion.TR4 && TeUsed)
                    {
                        width--;
                        height--;
                    }

                    y += (PageAndFlags & 0x7FFF) << 8;

                    if (!IsPowerOfTwo(width))
                        continue;
                    if (!IsPowerOfTwo(height))
                        continue;

                    if (width != height)
                        continue;

                    RelevantTextureInfos.Add((x, y, width, height));
                }
                else if (RectTexIndices.Contains((ushort)i))
                {
                    var (x, y, width, height) = GetBoundingBox4(ufixes);
                
                    if (width == 0 || height == 0)
                        continue; 
                
                    y += (PageAndFlags & 0x7FFF) << 8;
                
                    if (!IsPowerOfTwo(width))
                        width = NextPowerOfTwo(width);
                    if (!IsPowerOfTwo(height))
                        height = NextPowerOfTwo(height);
                    RelevantTextureInfos.Add((x, y, width, height));
                }
                else if (TriagTexIndices.Contains((ushort)i))
                {
                    bool TeUsed = (ufixes[0] & 0xff) == 0x7f;

                    var (x, y, width, height) = GetBoundingBox3(ufixes);
                
                    if (width == 0 || height == 0)
                        continue;

                    // TE Corrections
                    if (Version >= TrVersion.TR4 && TeUsed)
                        switch (mappingCorrection)
                        {
                            case 1:         x--;      break;
                            case 6: case 3:      y--; break;
                            case 2: case 7: x--; y--; break;
                            default:                  break;
                        }

                    y += (PageAndFlags & 0x7FFF) << 8;
                
                    if (!IsPowerOfTwo(width))
                        width = NextPowerOfTwo(width);
                    if (!IsPowerOfTwo(height))
                        height = NextPowerOfTwo(height);
                    RelevantTextureInfos.Add((x, y, width, height));
                }
            }

            RelevantTextureInfos = RelevantTextureInfos.Distinct().ToList();
        }

        private void ReadAnimatedTextures(BinaryReader levelReader)
        {
            var numAnimatedTextures = levelReader.ReadUInt32();

            var numRanges = levelReader.ReadUInt16();

            for (var i = 0; i < numRanges; i++)
            {
                var size = levelReader.ReadUInt16();
                for (var j = 0; j <= size; j++)
                {
                    AnimTexIndices.Add(levelReader.ReadUInt16());
                }
            }
        }

        // --- Palette and Atlas Reading ---

        private void ReadPaletteTr1(BinaryReader levelReader)
        {
            for (var i = 0; i < 256; i++)
            {
                palette24[i].r = levelReader.ReadByte();
                palette24[i].g = levelReader.ReadByte();
                palette24[i].b = levelReader.ReadByte();
            }
        }

        private void ReadAtlasTr4Tr5(BinaryReader reader)
        {
            var numRoomTiles = reader.ReadUInt16();
            var numObjectTiles = reader.ReadUInt16();
            var numBumpTiles = reader.ReadUInt16();

            // 32 bit textures
            var uncompressedSize = reader.ReadUInt32();
            var compressedSize = reader.ReadUInt32();
            atlas32 = reader.ReadBytes((int)compressedSize);
            atlas32 = ZLib.DecompressData(atlas32);
            numPages = (int)uncompressedSize / 65536 / 4;

            // 16 bit textures (not needed)
            uncompressedSize = reader.ReadUInt32();
            compressedSize = reader.ReadUInt32();
            reader.ReadBytes((int)compressedSize);

            // Misc textures (not needed?)
            uncompressedSize = reader.ReadUInt32();
            compressedSize = reader.ReadUInt32();
            reader.ReadBytes((int)compressedSize);
        }

        private void Read8BitAnd16BitAtlas(TrVersion Version, BinaryReader reader)
        {
            numPages = (int)reader.ReadUInt32();

            int numPixels = numPages * 65536;

            atlas8 = reader.ReadBytes(numPixels);

            if (Version == TrVersion.TR1)
                return;

            atlas16 = new ushort[numPixels];

            for (int i = 0; i < numPixels; i++)
                atlas16[i] = reader.ReadUInt16();
        }

        private void ReadPaletteTr2Tr3(BinaryReader reader)
        {
            for (int i = 0; i < 256; i++)
            {
                palette24[i].r = reader.ReadByte();
                palette24[i].g = reader.ReadByte();
                palette24[i].b = reader.ReadByte();
            }

            for (int i = 0; i < 256; i++)
            {
                palette32[i].r = reader.ReadByte();
                palette32[i].g = reader.ReadByte();
                palette32[i].b = reader.ReadByte();
                palette32[i].a = reader.ReadByte();
            }
        }

        // --- NG Handling ---

        private void CheckForNgHeader(BinaryReader reader)
        {
            var offset = reader.BaseStream.Position;
            reader.BaseStream.Seek(reader.BaseStream.Length - 8, SeekOrigin.Begin);
            var ngBuffer = reader.ReadBytes(4);
            if (ngBuffer[0] == 0x4E && ngBuffer[1] == 0x47 && ngBuffer[2] == 0x4C && ngBuffer[3] == 0x45)
                IsNg = true;
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        private string NgGetDecryptedLevelName(string fileName)
        {
            string levelName = Path.Combine(
                Path.GetDirectoryName(fileName)!, 
                Path.GetFileNameWithoutExtension(fileName) + "_decrypted" + Path.GetExtension(fileName));
            if (!TrngDecrypter.DecryptLevel(fileName, levelName))
                throw new Exception("Can't decrypt TRNG level " + fileName + ".");
            return levelName;
        }

        private void NgRemoveTempFile(string fileName)
        {
            File.SetAttributes(fileName, FileAttributes.Normal);
            File.Delete(fileName);
        }
    }
}
