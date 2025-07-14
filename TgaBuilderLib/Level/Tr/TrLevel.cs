using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Level
{
    public partial class TrLevel
    {
        private const int ORIGINAL_PAGE_SIZE = 256;
        private const int ORIGINAL_PAGE_PIXEL_COUNT = ORIGINAL_PAGE_SIZE * ORIGINAL_PAGE_SIZE;
        private const int PAGE_SIZE = 512;
        private const int ATLAS_MAX_HEIGHT = 32768;
        private const int IMPORT_BPP = 4;
        private bool IsNg;

        private int targetTrTexturePanelWidth;

        private long textureInfosStreamPosition;

        private List<ushort> RectTexIndices = new();
        private List<ushort> TriagTexIndices = new();
        private List<ushort> AnimTexIndices = new();

        private List<(int x, int y, int width, int height)> RelevantTextureInfos = new();
        private List<(int x, int y)> RepackedTexturePositions = new();

        private (byte r, byte g, byte b)[] palette24
            = new (byte r, byte g, byte b)[256];
        private (byte r, byte g, byte b, byte a)[] palette32
            = new (byte r, byte g, byte b, byte a)[256];

        private int numPages;
        private int numPagesRepacked;

        private byte[]? atlas8;
        private ushort[]? atlas16;
        private byte[]? atlas32;

        private byte[]? targetAtlas;
        private byte[]? targetAtlasRepacked;


        public WriteableBitmap ResultBitmap { get; private set; }

        public TrVersion Version { get; private set; }

        public bool BitmapSpaceSufficient { get; private set; } = true;

        public TrLevel(string fileName,            
            int trTexturePanelHorPagesNum = 2,
            bool useTrTextureRepacking = false)
        {
            targetTrTexturePanelWidth = trTexturePanelHorPagesNum * ORIGINAL_PAGE_SIZE;

            ReadLevel(fileName);

            if (Version == TrVersion.TRC)
                useTrTextureRepacking = false; // not supported currently

            RepackedTexturePositions = GetRepackedPositions(RelevantTextureInfos.Select(x => (x.width, x.height)).ToList());

            targetAtlas = Version >= TrVersion.TR4 ? atlas32 : new byte[numPages * ORIGINAL_PAGE_PIXEL_COUNT * IMPORT_BPP];

            if (Version == TrVersion.TR1)
                Atlas8ToAtlas32();
            else if (Version <= TrVersion.TR3)
                Atlas16ToAtlas32();

            targetAtlasRepacked = new byte[numPagesRepacked * targetTrTexturePanelWidth * targetTrTexturePanelWidth * IMPORT_BPP];
            RepackAtlas();

            if (numPagesRepacked == 0 || !useTrTextureRepacking)
            {
                int resHeight = ORIGINAL_PAGE_SIZE * numPages / trTexturePanelHorPagesNum;
                resHeight = NextHigherMultiple(resHeight, ORIGINAL_PAGE_SIZE);

                if (targetTrTexturePanelWidth != ORIGINAL_PAGE_SIZE)
                    targetAtlas = RearrangeImagePages(
                        inputData: targetAtlas!,
                        originalWidth: ORIGINAL_PAGE_SIZE,
                        blocksPerRow: trTexturePanelHorPagesNum);

                ResultBitmap = CreateWriteableBitmapFromByteArray(
                    byteArray: targetAtlas!,
                    width: targetTrTexturePanelWidth,
                    height: resHeight,
                    pixelFormat: PixelFormats.Bgra32);
            }
            else
                ResultBitmap = CreateWriteableBitmapFromByteArray(
                    byteArray: targetAtlasRepacked,
                    width: targetTrTexturePanelWidth,
                    height: targetTrTexturePanelWidth * numPagesRepacked,
                    pixelFormat: PixelFormats.Bgra32);
        }


        private void Atlas8ToAtlas32()
            { if (targetAtlas == null || atlas8 == null)
                throw new InvalidOperationException("The atlases must be loaded first.");
            int pixelCount = numPages * ORIGINAL_PAGE_PIXEL_COUNT;
            int index = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                (byte r, byte g, byte b) = palette24[atlas8[i]];
                targetAtlas[index++] = (byte)(b << 2);
                targetAtlas[index++] = (byte)(g << 2);
                targetAtlas[index++] = (byte)(r << 2);
                targetAtlas[index++] = 255; // Full opacity
            }
        }

        private void Atlas16ToAtlas32()
        {
            if (atlas16 == null || targetAtlas == null)
                throw new InvalidOperationException("The atlases must be loaded first.");
            int pixelCount = numPages * ORIGINAL_PAGE_PIXEL_COUNT;
            int index = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                ushort color = atlas16[i];
                targetAtlas[index++] = (byte)((color & 0x1F) << 3); // 5 bits blue
                targetAtlas[index++] = (byte)(((color >> 5) & 0x1F) << 3); // 5 bits green
                targetAtlas[index++] = (byte)(((color >> 10) & 0x1F) << 3); // 5 bits red
                targetAtlas[index++] = (byte)((color >> 15) * 255); // 1 bit alpha
            }
        }

        private void RepackAtlas()
        {
            if (RepackedTexturePositions.Count != RelevantTextureInfos.Count) 
                throw new ArgumentException("The number of original and repack tiles must be the same.");

            for (int i = 0; i < RelevantTextureInfos.Count; i++)
            {
                PlaceTile(i);
            }
        }

        public void PlaceTile(int idx)
        {
            int sourceIndex = (RelevantTextureInfos[idx].y * ORIGINAL_PAGE_SIZE + RelevantTextureInfos[idx].x) * IMPORT_BPP;
            int destinationIndex = (RepackedTexturePositions[idx].y * targetTrTexturePanelWidth + RepackedTexturePositions[idx].x) * IMPORT_BPP;
            int width = RelevantTextureInfos[idx].width;
            int height = RelevantTextureInfos[idx].height;

            for (int i = 0; i < height; i++)
            {
                if (destinationIndex + height * IMPORT_BPP > targetAtlasRepacked!.Length 
                    || sourceIndex + height * IMPORT_BPP > targetAtlas!.Length)
                    continue;
                Array.Copy(targetAtlas, sourceIndex, targetAtlasRepacked, destinationIndex, width * IMPORT_BPP);
                destinationIndex += targetTrTexturePanelWidth * IMPORT_BPP;
                sourceIndex += ORIGINAL_PAGE_SIZE * IMPORT_BPP;
            }
        }

        private WriteableBitmap CreateWriteableBitmapFromByteArray(byte[] byteArray, int width, int height, PixelFormat pixelFormat)
        {
            int bitsPerPixel = pixelFormat.BitsPerPixel;
            int stride = (width * bitsPerPixel + 7) / 8;
            int actualHeight = Math.Min(height, ATLAS_MAX_HEIGHT);
            int expectedSize = stride * height;
            int croppedSize = stride * actualHeight;

            if (byteArray.Length < expectedSize)
                throw new ArgumentException("The byte array is smaller than the expected size for the given width and height.");

            if (height > ATLAS_MAX_HEIGHT)
                BitmapSpaceSufficient = false; 

            // WriteableBitmap with capped height
            WriteableBitmap writeableBitmap = new WriteableBitmap(width, actualHeight, 96, 96, pixelFormat, null);

            writeableBitmap.Lock();

            // Only copy the necessary part of the byte array
            IntPtr backBuffer = writeableBitmap.BackBuffer;
            Marshal.Copy(byteArray, 0, backBuffer, croppedSize);

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, actualHeight));
            writeableBitmap.Unlock();

            return writeableBitmap;
        }

        public static byte[] RearrangeImagePages(
            byte[] inputData,
            int originalWidth,
            int blocksPerRow)
        {
            if (originalWidth != 256)
                throw new ArgumentException("Original width must be 256 pixels.");

            const int blockWidth = 256;
            const int blockHeight = 256;

            int totalPixels = inputData.Length / IMPORT_BPP;
            int originalHeight = totalPixels / originalWidth;

            if (originalHeight % blockHeight != 0)
                throw new ArgumentException("Height must be a multiple of 256.");

            int actualBlocks = originalHeight / blockHeight;

            // Calculate rows, rounded
            int blockRows = (int)Math.Ceiling((double)actualBlocks / blocksPerRow);
            int totalBlocks = blockRows * blocksPerRow; 

            int newWidth = blockWidth * blocksPerRow;
            int newHeight = blockHeight * blockRows;

            byte[] outputData = new byte[newWidth * newHeight * IMPORT_BPP]; 

            for (int block = 0; block < actualBlocks; block++)
            {
                int blockRow = block / blocksPerRow;
                int blockCol = block % blocksPerRow;

                for (int y = 0; y < blockHeight; y++)
                {
                    for (int x = 0; x < blockWidth; x++)
                    {
                        int srcX = x;
                        int srcY = block * blockHeight + y;
                        int srcIndex = (srcY * originalWidth + srcX) * IMPORT_BPP;

                        int dstX = blockCol * blockWidth + x;
                        int dstY = blockRow * blockHeight + y;
                        int dstIndex = (dstY * newWidth + dstX) * IMPORT_BPP;

                        Buffer.BlockCopy(inputData, srcIndex, outputData, dstIndex, IMPORT_BPP);
                    }
                }
            }
            return outputData;
        }
    }
}
