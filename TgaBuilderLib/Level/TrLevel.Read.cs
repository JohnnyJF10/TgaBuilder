using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Buffers;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading;
using TgaBuilderLib.Enums;
using TgaBuilderLib.Utils;

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
                _isNg = false;
                if (Version == TrVersion.TR4)
                    CheckForNgHeader(reader);

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
            reader.BaseStream.Position = _textureInfosStreamPosition;
            ReadTextureInfos(reader, cancellationToken);
        }

        // --- Rooms Reading ---

        private void ReadRooms(TrVersion Version, BinaryReader levelReader, CancellationToken? cancellationToken = null)
        {
            var numRooms = Version != TrVersion.TRC ? levelReader.ReadUInt16() : levelReader.ReadUInt32();
            for (var i = 0; i < numRooms; i++)
            {
                cancellationToken?.ThrowIfCancellationRequested();
                if (Version != TrVersion.TRC)
                {
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
                else
                {
                    var xela = System.Text.Encoding.ASCII.GetString(levelReader.ReadBytes(4));
                    var roomDataSize = levelReader.ReadUInt32();
                    levelReader.ReadBytes((int)roomDataSize);
                }
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
                throw new FileFormatException("TR1 atlas data is incomplete or corrupted.");

            if (_atlasTr1 == null || _atlasTr1.Length == 0)
                throw new FileFormatException("TR1 atlas data is missing or empty.");

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
                throw new FileFormatException("TR1 atlas data is incomplete or corrupted.");
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
                throw new FileFormatException("TR4-5 atlas data is incomplete or corrupted.");

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

        private void CheckForNgHeader(BinaryReader reader)
        {
            var offset = reader.BaseStream.Position;
            reader.BaseStream.Seek(reader.BaseStream.Length - 8, SeekOrigin.Begin);
            var ngBuffer = reader.ReadBytes(4);
            if (ngBuffer[0] == 0x4E && ngBuffer[1] == 0x47 && ngBuffer[2] == 0x4C && ngBuffer[3] == 0x45)
                _isNg = true;
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        private string NgGetDecryptedLevelName(string fileName)
        {
            if (_trngDecrypter is null)
                throw new ArgumentNullException(nameof(_trngDecrypter), "TRNG decrypter is not initialized.");

            string levelName = Path.Combine(
                Path.GetDirectoryName(fileName)!, 
                Path.GetFileNameWithoutExtension(fileName) + "_decrypted.temp");

            if (!_trngDecrypter.DecryptLevel(fileName, levelName))
                throw new FileFormatException("Failed to decrypt the level file: " + fileName);

            return levelName;
        }

        private void NgRemoveTempFile(string fileName)
        {
            File.SetAttributes(fileName, FileAttributes.Normal);
            File.Delete(fileName);
        }
    }
}
