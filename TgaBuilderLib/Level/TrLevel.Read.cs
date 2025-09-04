using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Level
{
    public partial class TrLevel
    {
        protected override void ReadLevel(string fileName, CancellationToken? cancellationToken = null)
        {

            long uncompressedEndPos = 0;

            uint uncompressedSize = 0;
            uint compressedSize = 0;

            bool needsDecrypt = false;
            string levelName = fileName;

            byte[] levelData = new byte[0];

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
                    _ => throw new FormatException("Unknown game version 0x" + versionUInt.ToString("X") + ".")
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

                if (Version == TrVersion.TR1)
                {
                    // Read 8bit atlas for TR1, palette is at the end of the file
                    Read8BitAtlasTr1(reader);
                    ReadLevelData(reader);
                }

                // Read the texture data for TR2 and TR3
                if (Version == TrVersion.TR2 || Version == TrVersion.TR3)
                {
                    // Palette data not required for TR2-3. Skip (3 + 4) * 256 bytes = 1792 bytes.
                    reader.ReadBytes(1792);
                    ReadAtlasTr2Tr3(reader, cancellationToken);
                    if (_useTrTextureRepacking)
                        ReadLevelData(reader, cancellationToken);
                }

                uncompressedEndPos = reader.BaseStream.Position;

                // Read 16 and 32 bit textures, uncompress and read data if TR4 and TRC
                if (Version >= TrVersion.TR4)
                {
                    ReadAtlasTr4Tr5(reader, cancellationToken);

                    if (Version == TrVersion.TRC)
                        reader.ReadBytes(32);

                    uncompressedSize = reader.ReadUInt32();
                    compressedSize = reader.ReadUInt32();

                    if (Version == TrVersion.TR4)
                        using (var dataStream = DecompressStream(reader.BaseStream, compressedSize))
                        using (var dataReader = new BinaryReader(dataStream))
                            ReadLevelData(dataReader);
                    else // TRC: No compression
                        ReadLevelData(reader);
                }

                if (needsDecrypt)
                    _ = Task.Run(async () => { await Task.Delay(2000); NgRemoveTempFile(levelName); });
            }
        }

        private void ReadLevelData(BinaryReader reader, CancellationToken? cancellationToken = null)
        {
            // 32-bit unused value 
            reader.ReadUInt32();

            // Read rooms
            if (Version == TrVersion.TRC)
                ReadRoomsTRC(reader, cancellationToken);
            else
                ReadRooms(Version, reader, cancellationToken);

            // Floordata
            var numFloorData = reader.ReadUInt32();
            reader.ReadBytes((int)numFloorData * 2);

            // Mesh data
            var numMeshData = reader.ReadUInt32();
            reader.ReadBytes((int)numMeshData * 2);

            var numMeshPointers = reader.ReadUInt32();
            reader.ReadBytes((int)numMeshPointers * 4);

            // Animations
            var numAnimations = reader.ReadUInt32();
            if (Version <= TrVersion.TR3)
                reader.ReadBytes((int)numAnimations * 32);
            else
                reader.ReadBytes((int)numAnimations * 40);

            // State changes
            var numStateChanges = reader.ReadUInt32();
            reader.ReadBytes((int)numStateChanges * 6);

            // Anim dispatches
            var numDispatches = reader.ReadUInt32();
            reader.ReadBytes((int)numDispatches * 8);


            // Anim commands
            var numAnimCommands = reader.ReadUInt32();
            reader.ReadBytes((int)numAnimCommands * 2);


            // Mesh trees
            var numMeshTrees = reader.ReadUInt32();
            reader.ReadBytes((int)numMeshTrees * 4);

            // Keyframes
            var numFrames = reader.ReadUInt32();
            reader.ReadBytes((int)numFrames * 2);

            // Moveables
            var numMoveables = reader.ReadUInt32();
            if (Version == TrVersion.TRC)
                reader.ReadBytes((int)numMoveables * 20);
            else
                reader.ReadBytes((int)numMoveables * 18);

            // Static meshes
            var numStaticMeshes = reader.ReadUInt32();
            reader.ReadBytes((int)numStaticMeshes * 32);

            // If Version <= TR2 object textures are here
            // Anim Infos have not been read yet, so we need to
            // store the reader position and come here again later
            if (Version == TrVersion.TR1 || Version == TrVersion.TR2)
            {
                _textureInfosStreamPosition = reader.BaseStream.Position;

                var numObjectTextures = reader.ReadUInt32();
                reader.ReadBytes((int)numObjectTextures * 20);
            }

            // SPR marker
            var marker = "";
            if (Version == TrVersion.TR4)
                marker = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(3));
            if (Version == TrVersion.TRC)
                marker = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));

            // Sprite textures
            var numSpriteTextures = reader.ReadUInt32();
            reader.ReadBytes((int)numSpriteTextures * 16);

            // Sprite sequences
            var numSpriteSequences = reader.ReadUInt32();
            reader.ReadBytes((int)numSpriteSequences * 8);

            // Cameras
            var numCameras = reader.ReadUInt32();
            reader.ReadBytes((int)numCameras * 16);

            // Flyby cameras
            if (Version == TrVersion.TR4 || Version == TrVersion.TRC)
            {
                var numFlybyCameras = reader.ReadUInt32();
                reader.ReadBytes((int)numFlybyCameras * 40);
            }

            // Sound sources
            var numSoundSources = reader.ReadUInt32();
            reader.ReadBytes((int)numSoundSources * 16);

            // Boxes
            var numBoxes = reader.ReadUInt32();
            reader.ReadBytes((int)numBoxes * (Version == TrVersion.TR1 ? 20 : 8));

            // Overlaps
            var numOverlaps = reader.ReadUInt32();
            reader.ReadBytes((int)numOverlaps * 2);

            // Zones
            reader.ReadBytes((int)numBoxes * (Version == TrVersion.TR1 ? 12 : 20));

            // Animated textures
            ReadAnimatedTextures(reader, cancellationToken);

            // If Version >= TR3, object textures are here, amis already red, so afterwards we are finished
            if (Version == TrVersion.TR3 || Version == TrVersion.TR4 || Version == TrVersion.TRC)
            {
                if (Version == TrVersion.TR4)
                    marker = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (Version == TrVersion.TRC)
                    marker = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(5));

                ReadTextureInfos(reader, cancellationToken);

                return; // Finished reading level for TR3, TR4 and TRC
            }

            // Items
            var numEntities = reader.ReadUInt32();
            reader.ReadBytes((int)numEntities * (Version == TrVersion.TR1 ? 22 : 24));

            if (Version == TrVersion.TR1 || Version == TrVersion.TR2)
            {
                // Lightmap
                reader.ReadBytes(8192);

                // Palette
                if (Version == TrVersion.TR1)
                {
                    ReadPaletteAndGetAtlasTr1(reader, cancellationToken);
                }
            }

            // If Version is TR1 or TR2, we need to read the object textures
            // as the animated textures infos are available now
            reader.BaseStream.Seek(_textureInfosStreamPosition, SeekOrigin.Begin);
            ReadTextureInfos(reader, cancellationToken);
        }

        // --- Rooms Reading ---

        private void ReadRooms(TrVersion Version, BinaryReader levelReader, CancellationToken? cancellationToken = null)
        {
            var numRooms = levelReader.ReadUInt16();
            for (var i = 0; i < numRooms; i++)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                // Room info
                levelReader.ReadBytes(16);

                ReadRoomData(levelReader, cancellationToken);

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
        }

        private void ReadRoomsTRC(BinaryReader levelReader, CancellationToken? cancellationToken = null)
        {
            uint tr5ValueCheck;

            var numRooms = levelReader.ReadUInt32();
            for (var i = 0; i < numRooms; i++)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                // Read room header marker (should be "XELA", 0x414c4558)
                var xela = System.Text.Encoding.ASCII.GetString(levelReader.ReadBytes(4));
                if (string.Compare(xela, "XELA", StringComparison.Ordinal) != 0)
                    throw new FormatException("Invalid room header marker: " + xela);

                // Total size of the room block
                var roomDataSize = levelReader.ReadUInt32();

                // Store current stream position to later skip to the end of this room block
                int startPos = (int)levelReader.BaseStream.Position;

                // --- ROOM HEADER FIELDS --- //

                // Separator (0xCDCDCDCD)
                levelReader.ReadUInt32();

                // EndSDOffset
                levelReader.ReadUInt32();

                // StartSDOffset
                levelReader.ReadUInt32();

                // Separator (0 or 0xCDCDCDCD)
                levelReader.ReadUInt32();

                // EndPortalOffset
                levelReader.ReadUInt32();

                // tr_room_info fields
                var roomX = levelReader.ReadInt32();     // Room position X
                var roomY = levelReader.ReadInt32();     // Room position Y
                var roomZ = levelReader.ReadInt32();     // Room position Z
                var roomYbottom = levelReader.ReadInt32(); // Bottom Y (floor level)
                var roomYtop = levelReader.ReadInt32();    // Top Y (ceiling level)

                // Sector grid dimensions
                var numZsectors = levelReader.ReadUInt16();
                var numXsectors = levelReader.ReadUInt16();

                // Room color in ARGB format
                var roomColor = levelReader.ReadUInt32();

                // Light and static mesh counts
                var numLights = levelReader.ReadUInt16();
                var numStatics = levelReader.ReadUInt16();

                // ReverbInfo (1 byte), AlternateGroup (1 byte), WaterScheme (2 bytes)
                levelReader.ReadUInt32(); // Skipped as combined 4 bytes

                // Filler[2] (both 0x00007FFF)
                levelReader.ReadUInt32();
                levelReader.ReadUInt32();

                // Separator[2] (both 0xCDCDCDCD)
                levelReader.ReadUInt32();

                levelReader.ReadUInt32();

                // Filler (always 0xFFFFFFFF)
                levelReader.ReadUInt32();

                // Alternate room index (-1 if none)
                var alternateRoom = levelReader.ReadUInt16();

                // Room flags
                var flags = levelReader.ReadUInt16();

                // Unknown values
                levelReader.ReadUInt32(); // Unknown1
                levelReader.ReadUInt32(); // Unknown2 (always 0)
                levelReader.ReadUInt32(); // Unknown3 (always 0)

                // Separator (null room = 0, normal = 0xCDCDCDCD)
                tr5ValueCheck = levelReader.ReadUInt32();
                if (tr5ValueCheck == 0)
                {
                    // --- SKIP TO NEXT ROOM --- //
                    // Move back to start and skip whole roomDataSize block to get ready for next room
                    levelReader.BaseStream.Seek(startPos, SeekOrigin.Begin);
                    levelReader.BaseStream.Seek(roomDataSize, SeekOrigin.Current);

                    continue;
                }

                // Unknown4 and Unknown5
                levelReader.ReadUInt16();
                levelReader.ReadUInt16();

                // Room position (floating point precision)
                var roomXfloat = levelReader.ReadSingle();
                var roomYfloat = levelReader.ReadSingle();
                var roomZfloat = levelReader.ReadSingle();

                // Separator[4] (0xCDCDCDCD each)
                tr5ValueCheck = levelReader.ReadUInt32();
                tr5ValueCheck = levelReader.ReadUInt32();
                tr5ValueCheck = levelReader.ReadUInt32();
                tr5ValueCheck = levelReader.ReadUInt32();


                // Separator (normal = 0, null room = 0xCDCDCDCD)
                tr5ValueCheck = levelReader.ReadUInt32();
                if (tr5ValueCheck == 0xCDCDCDCD)
                {
                    // --- SKIP TO NEXT ROOM --- //
                    // Move back to start and skip whole roomDataSize block to get ready for next room
                    levelReader.BaseStream.Seek(startPos, SeekOrigin.Begin);
                    levelReader.BaseStream.Seek(roomDataSize, SeekOrigin.Current);

                    continue;
                }

                // Separator (0xCDCDCDCD)
                levelReader.ReadUInt32();

                // Number of quads and triangles in the room
                var numQuads = levelReader.ReadUInt32();
                var numTriangles = levelReader.ReadUInt32();
                if (numQuads == 0xCDCDCDCD || numTriangles == 0xCDCDCDCD)
                {
                    // --- SKIP TO NEXT ROOM --- //
                    // Move back to start and skip whole roomDataSize block to get ready for next room
                    levelReader.BaseStream.Seek(startPos, SeekOrigin.Begin);
                    levelReader.BaseStream.Seek(roomDataSize, SeekOrigin.Current);

                    continue;
                }

                // Pointer to lights (tr5_room_data.Lights)
                levelReader.ReadUInt32();

                // Size of light block in bytes
                var lightSize = levelReader.ReadUInt32();

                // Duplicate of numLights (always the same)
                var numLights2 = levelReader.ReadUInt32();

                // Number of fog bulbs
                int numFogBulbs = (int)levelReader.ReadUInt32();

                // RoomYTop and RoomYBottom again?
                levelReader.ReadUInt32();
                levelReader.ReadUInt32();

                // Number of room layers (volumes)
                var numLayers = levelReader.ReadUInt32();

                // Layer offset
                levelReader.ReadUInt32();

                // Vertices offset
                levelReader.ReadUInt32();

                // PolyOffset
                levelReader.ReadUInt32();

                // PolyOffset2 (should be same as PolyOffset)
                levelReader.ReadUInt32();

                // Number of room vertices
                var numVertices = levelReader.ReadUInt32();

                // Separator[4] (0xCDCDCDCD each)
                levelReader.ReadUInt32();
                levelReader.ReadUInt32();
                levelReader.ReadUInt32();
                levelReader.ReadUInt32();

                // --- ROOM DATA FIELDS --- //

                // Read light data (88 bytes * numLights)
                levelReader.ReadBytes(88 * numLights);

                // Read fog bulb data (each 16 bytes)
                levelReader.ReadBytes(36 * numFogBulbs);

                // Read sectors (8 bytes each = sizeof(tr_room_sector))
                levelReader.ReadBytes(8 * numXsectors * numZsectors);

                // Read portals
                var numPortals = levelReader.ReadUInt16(); // Number of portals
                levelReader.ReadBytes(32 * numPortals); // Read portal data (each 32 bytes)

                // Read separator (0xCDCD)
                var ets = levelReader.ReadUInt16();
                if (ets != 0xCDCD)
                    throw new FormatException("Invalid separator after portals: " + ets.ToString("X"));

                // Read static mesh list (each = 20 bytes)
                levelReader.ReadBytes(20 * numStatics);

                // Read layers (each = 56 bytes)
                levelReader.ReadBytes(56 * (int)numLayers);

                // Read faces:
                // - Quads are tr_face4 (20 bytes each)
                // - Triangles are tr_face3 (16 bytes each)
                int faceDataSize = (int)(numQuads * 20 + numTriangles * 16);

                for (var j = 0; j < numQuads; j++)
                {
                    cancellationToken?.ThrowIfCancellationRequested();
                
                    levelReader.ReadBytes(8); // Vertices
                    _rectTexIndices.Add(levelReader.ReadUInt16()); // Texture
                    levelReader.ReadUInt16(); // Zero padding
                }
                
                for (var j = 0; j < numTriangles; j++)
                {
                    cancellationToken?.ThrowIfCancellationRequested();
                
                    levelReader.ReadBytes(6); // Vertices
                    _triagTexIndices.Add(levelReader.ReadUInt16()); // Texture
                    levelReader.ReadUInt16(); // Zero padding
                }

                // --- SKIP TO NEXT ROOM --- //
                // Move back to start and skip whole roomDataSize block to get ready for next room
                levelReader.BaseStream.Seek(startPos, SeekOrigin.Begin);
                levelReader.BaseStream.Seek(roomDataSize, SeekOrigin.Current);
            }
        }

        private void ReadRoomData(BinaryReader levelReader, CancellationToken? cancellationToken = null)
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
                cancellationToken?.ThrowIfCancellationRequested();

                levelReader.ReadBytes(8); // Vertices
                _rectTexIndices.Add(levelReader.ReadUInt16()); // Texture
            }

            var numTriangles = levelReader.ReadUInt16();
            for (var i = 0; i < numTriangles; i++)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                levelReader.ReadBytes(6); // Vertices
                _triagTexIndices.Add(levelReader.ReadUInt16()); // Texture
            }

            var numSprites = levelReader.ReadUInt16();
            levelReader.ReadBytes(numSprites * 4);
        }

        // --- Texture Infos Reading ---

        private void ReadTextureInfos(BinaryReader levelReader, CancellationToken? cancellationToken = null)
        {
            var ufixes = new ushort[8];
            var cornerPoints = new int[8];
            int mappingCorrection = 0;

            var numObjectTextures = levelReader.ReadUInt32();
            for (var i = 0; i < numObjectTextures; i++)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                levelReader.ReadUInt16(); // Attributes
                ushort PageAndFlags = levelReader.ReadUInt16();

                if (Version >= TrVersion.TR4)
                    mappingCorrection = levelReader.ReadUInt16() & 0xF;

                for (int j = 0; j < 8; j++)
                {
                    ufixes[j] = levelReader.ReadUInt16();
                    cornerPoints[j] = UFixed16ToUShort(ufixes[j]);
                }

                if (Version == TrVersion.TR4)
                    levelReader.ReadBytes(16);
                if (Version == TrVersion.TRC)
                    levelReader.ReadBytes(18);

                if (_animTexIndices.Contains((ushort)i))
                {
                    bool TeUsed = (ufixes[0] & 0xff) == 0x7f;

                    var (x, y, width, height) = GetBoundingBox4(cornerPoints);

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

                    _relevantTextureInfos.Add((x, y, width, height));
                }
                else if (_rectTexIndices.Contains((ushort)i))
                {
                    var (x, y, width, height) = GetBoundingBox4(cornerPoints);

                    if (width == 0 || height == 0)
                        continue;

                    y += (PageAndFlags & 0x7FFF) << 8;

                    if (!IsPowerOfTwo(width))
                        width = NextPowerOfTwo(width);
                    if (!IsPowerOfTwo(height))
                        height = NextPowerOfTwo(height);
                    _relevantTextureInfos.Add((x, y, width, height));
                }
                else if (_triagTexIndices.Contains((ushort)i))
                {
                    bool TeUsed = (ufixes[0] & 0xff) == 0x7f;

                    var (x, y, width, height) = GetBoundingBox3(cornerPoints);

                    if (width == 0 || height == 0)
                        continue;

                    // TE Corrections
                    if (Version >= TrVersion.TR4 && TeUsed)
                        switch (mappingCorrection)
                        {
                            case 1: x--; break;
                            case 6: case 3: y--; break;
                            case 2: case 7: x--; y--; break;
                            default: break;
                        }

                    y += (PageAndFlags & 0x7FFF) << 8;

                    if (!IsPowerOfTwo(width))
                        width = NextPowerOfTwo(width);
                    if (!IsPowerOfTwo(height))
                        height = NextPowerOfTwo(height);
                    _relevantTextureInfos.Add((x, y, width, height));
                }
            }

            _relevantTextureInfos = _relevantTextureInfos.Distinct().ToList();
        }
        private void ReadAnimatedTextures(BinaryReader levelReader, CancellationToken? cancellationToken = null)
        {
            var numAnimatedTextures = levelReader.ReadUInt32();

            int checksum = 0;

            var numRanges = levelReader.ReadUInt16();
            checksum += 2;

            for (var i = 0; i < numRanges; i++)
            {
                var size = levelReader.ReadUInt16();
                checksum += 2;
                for (var j = 0; j <= size; j++)
                {
                    cancellationToken?.ThrowIfCancellationRequested();

                    _animTexIndices.Add(levelReader.ReadUInt16());
                    checksum += 2;
                }
            }

            int expectedChecksum = (int)(numAnimatedTextures * 2);

            // weird issue in DXTRE3D Levels, where the checksum is not correct
            if (checksum < expectedChecksum)
                levelReader.ReadBytes(expectedChecksum - checksum);

        }

        private ushort UFixed16ToUShort(ushort ufixed16)
        {
            int fractionalPart = (ufixed16 & 0xFF) >= 0x7F ? 1 : 0;
            int integerPart = ufixed16 >> 8;

            ushort result = (ushort)(integerPart + fractionalPart);
            return result;
        }

        // --- Palette and Atlas Reading ---

        private void ReadPaletteAndGetAtlasTr1(BinaryReader levelReader, CancellationToken? cancellationToken = null)
        {
            // 256 * 3 = 768 bytes for 256 colors
            _paletteTr1 = _bytePool.Rent(768);
            int bytesRead = levelReader.Read(_paletteTr1, 0, 768);

            if (bytesRead != 768)
                throw new FormatException("TR1 atlas data is incomplete or corrupted.");

            if (_atlasTr1 == null || _atlasTr1.Length == 0)
                throw new FormatException("TR1 atlas data is missing or empty.");

            int pixelCount = _numPages * ORIGINAL_PAGE_PIXEL_COUNT;
            int index = 0;

            _rawAtlas = _bytePool.Rent(pixelCount * IMPORT_BPP);

            try
            {
                for (int i = 0; i < pixelCount; i++)
                {
                    cancellationToken?.ThrowIfCancellationRequested();

                    var atlas8val = _atlasTr1[i];

                    _rawAtlas[index++] = (byte)(_paletteTr1[3 * atlas8val + 2] << 2); // r
                    _rawAtlas[index++] = (byte)(_paletteTr1[3 * atlas8val + 1] << 2); // g
                    _rawAtlas[index++] = (byte)(_paletteTr1[3 * atlas8val    ] << 2); // b
                    _rawAtlas[index++] = (byte)(atlas8val == 0 ? 0 : 255); // a
                }
            }
            finally
            {
                _bytePool.Return(_paletteTr1);
                _paletteTr1 = null;
                _bytePool.Return(_atlasTr1);
                _atlasTr1 = null;
            }
        }

        private void Read8BitAtlasTr1(BinaryReader reader)
        {
            _numPages = (int)reader.ReadUInt32();

            int numPixels = _numPages * ORIGINAL_PAGE_PIXEL_COUNT;

            _atlasTr1 = _bytePool.Rent(numPixels);

            int bytesRead = reader.Read(_atlasTr1, 0, numPixels);

            if (bytesRead != numPixels)
                throw new FormatException("TR1 atlas data is incomplete or corrupted.");
        }

        private void ReadAtlasTr2Tr3(BinaryReader reader, CancellationToken? cancellationToken = null)
        {
            _numPages = (int)reader.ReadUInt32();
            int pixelCount = _numPages * ORIGINAL_PAGE_PIXEL_COUNT;

            // 8-bit palette indices not required for TR2-3
            reader.ReadBytes(pixelCount); 

            _rawAtlas = _bytePool.Rent(pixelCount * IMPORT_BPP); 

            int index = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                ushort color = reader.ReadUInt16();

                _rawAtlas[index++] = (byte)((color & 0x1F) << 3); // 5 bits blue
                _rawAtlas[index++] = (byte)(((color >> 5) & 0x1F) << 3); // 5 bits green
                _rawAtlas[index++] = (byte)(((color >> 10) & 0x1F) << 3); // 5 bits red
                _rawAtlas[index++] = (byte)((color >> 15) * 255); // 1 bit alpha
            }
        }

        private void ReadAtlasTr4Tr5(BinaryReader reader, CancellationToken? cancellationToken = null)
        {
            var numRoomTiles = reader.ReadUInt16();
            var numObjectTiles = reader.ReadUInt16();
            var numBumpTiles = reader.ReadUInt16();

            // 32 bit textures
            var uncompressedSize = reader.ReadUInt32();
            var compressedSize = reader.ReadUInt32();

            _rawAtlasCompressed = _bytePool.Rent((int)compressedSize);
            int bytesRead = reader.Read(_rawAtlasCompressed, 0, (int)compressedSize);

            if (bytesRead != compressedSize)
                throw new FormatException("TR4-5 atlas data is incomplete or corrupted.");

            _rawAtlas = DecompressZlib(_rawAtlasCompressed, cancellationToken);
            _numPages = (int)uncompressedSize / ORIGINAL_PAGE_PIXEL_COUNT / IMPORT_BPP;

            cancellationToken?.ThrowIfCancellationRequested();

            // 16 bit textures (not needed)
            uncompressedSize = reader.ReadUInt32();
            compressedSize = reader.ReadUInt32();
            reader.ReadBytes((int)compressedSize);

            // Misc textures (not needed?)
            uncompressedSize = reader.ReadUInt32();
            compressedSize = reader.ReadUInt32();
            reader.ReadBytes((int)compressedSize);

            _bytePool.Return(_rawAtlasCompressed);
            _rawAtlasCompressed = null;
        }

        private byte[] DecompressZlib(byte[] compressedData, CancellationToken? cancellationToken = null)
        {
            var bufferSize = 81920; // 80 KB 
            byte[] rentedBuffer = _bytePool.Rent(bufferSize);
            byte[] decompressedBuffer = _bytePool.Rent(compressedData.Length * 4); 
            int totalRead = 0;

            try
            {
                using (var inputStream = new MemoryStream(compressedData))
                using (var inflaterStream = new InflaterInputStream(inputStream))
                {
                    int bytesRead;
                    while ((bytesRead = inflaterStream.Read(rentedBuffer, 0, rentedBuffer.Length)) > 0)
                    {
                        cancellationToken?.ThrowIfCancellationRequested();

                        if (totalRead + bytesRead > decompressedBuffer.Length)
                        {
                            int newSize = decompressedBuffer.Length * 2;
                            byte[] newBuffer = _bytePool.Rent(newSize);
                            Buffer.BlockCopy(decompressedBuffer, 0, newBuffer, 0, totalRead);
                            _bytePool.Return(decompressedBuffer);
                            decompressedBuffer = newBuffer;
                        }

                        Buffer.BlockCopy(rentedBuffer, 0, decompressedBuffer, totalRead, bytesRead);
                        totalRead += bytesRead;
                    }
                }
                return decompressedBuffer;
            }
            catch
            {
                _bytePool.Return(decompressedBuffer);
                throw;
            }
            finally
            {
                _bytePool.Return(rentedBuffer);
            }
        }


        // --- NG Handling ---

        private string NgGetDecryptedLevelName(string fileName)
        {
            if (_trngDecrypter is null)
                throw new ArgumentNullException(nameof(_trngDecrypter), "TRNG decrypter is not initialized.");

            string levelName = Path.Combine(
                Path.GetDirectoryName(fileName)!, 
                Path.GetFileNameWithoutExtension(fileName) + "_decrypted.temp");

            if (!_trngDecrypter.DecryptLevel(fileName, levelName))
                throw new FormatException("Failed to decrypt the level file: " + fileName);

            return levelName;
        }

        private void NgRemoveTempFile(string fileName)
        {
            File.SetAttributes(fileName, FileAttributes.Normal);
            File.Delete(fileName);
        }
    }
}
