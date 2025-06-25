using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using THelperLib.Abstraction;

namespace THelperLib.Tr
{
    public partial class TrLevel
    {
        private const int ORIGINAL_PAGE_SIZE = 256;
        private const int ORIGINAL_PAGE_PIXEL_COUNT = ORIGINAL_PAGE_SIZE * ORIGINAL_PAGE_SIZE;
        private const int PAGE_SIZE = 512;
        private const int ATLAS_MAX_HEIGHT = 32768;

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

        private byte[]? atlas24;
        private byte[]? atlas24Repacked;


        public WriteableBitmap ResultBitmap { get; private set; }

        public TrVersion Version { get; private set; }

        public bool BitmapSpaceSufficient { get; private set; } = true;

        public TrLevel(string fileName,            
            int trTexturePanelHorPagesNum = 2,
            bool useTrTextureRepacking = false)
        {
            targetTrTexturePanelWidth = trTexturePanelHorPagesNum * ORIGINAL_PAGE_SIZE;

            ReadLevel(fileName);

            RepackedTexturePositions = RepackAtlas(RelevantTextureInfos.Select(x => (x.width, x.height)).ToList());

            atlas24 = new byte[numPages * ORIGINAL_PAGE_PIXEL_COUNT * 3];

            if (Version == TrVersion.TR1)
                Atlas8ToAtlas24();
            else if (Version <= TrVersion.TR3)
                Atlas16ToAtlas24();
            else
                Atlas32ToAtlas24();

            atlas24Repacked = new byte[numPagesRepacked * targetTrTexturePanelWidth * targetTrTexturePanelWidth * 3];
            RepackAtlas();

            if (numPagesRepacked == 0 || !useTrTextureRepacking)
            {
                int resHeight = ORIGINAL_PAGE_SIZE * numPages / trTexturePanelHorPagesNum;
                resHeight = NextHigherMultiple(resHeight, ORIGINAL_PAGE_SIZE);

                if (targetTrTexturePanelWidth != ORIGINAL_PAGE_SIZE)
                    atlas24 = RearrangeImagePages(
                        inputData: atlas24,
                        originalWidth: ORIGINAL_PAGE_SIZE,
                        blocksPerRow: trTexturePanelHorPagesNum);

                ResultBitmap = CreateWriteableBitmapFromByteArray(
                    byteArray: atlas24,
                    width: targetTrTexturePanelWidth,
                    height: resHeight,
                    pixelFormat: PixelFormats.Rgb24);
            }
            else
                ResultBitmap = CreateWriteableBitmapFromByteArray(
                    byteArray: atlas24Repacked,
                    width: targetTrTexturePanelWidth,
                    height: targetTrTexturePanelWidth * numPagesRepacked,
                    pixelFormat: PixelFormats.Rgb24);
        }

        private void Atlas32ToAtlas24()
        {
            atlas24 = new byte[atlas32!.Length / 4 * 3];
            int sourceIndex = 0;
            int destinationIndex = 0;
            for (int i = 0; i < atlas32.Length / 4; i++)
            {
                atlas24[destinationIndex] = atlas32[sourceIndex + 2];
                atlas24[destinationIndex + 1] = atlas32[sourceIndex + 1];
                atlas24[destinationIndex + 2] = atlas32[sourceIndex];
                sourceIndex += 4;
                destinationIndex += 3;
            }
        }

        private void Atlas8ToAtlas24()
        {
            if (atlas8 == null) 
                throw new InvalidOperationException("The 32-bit atlas must be loaded first.");

            int pixelCount = numPages * ORIGINAL_PAGE_PIXEL_COUNT;
            int index = 0;

            for (int i = 0; i < pixelCount; i++)
            {
                (byte r, byte g, byte b) = palette24[atlas8[i]];
                atlas24![index++] = (byte)(r << 2);
                atlas24![index++] = (byte)(g << 2);
                atlas24![index++] = (byte)(b << 2);
            }
        }

        private void Atlas16ToAtlas24()
        {
            if (atlas16 == null)
                throw new InvalidOperationException("The 16-bit atlas must be loaded first.");
            int pixelCount = numPages * ORIGINAL_PAGE_PIXEL_COUNT;
            int index = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                ushort color = atlas16[i];
                //int a = (color >> 15) * 255; // 1 bit alpha
                atlas24![index++] = (byte)(((color >> 10) & 0x1F) << 3); // 5 bits red
                atlas24![index++] = (byte)(((color >> 5) & 0x1F) << 3); // 5 bits green
                atlas24![index++] = (byte)((color & 0x1F) << 3); // 5 bits blue
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
            int sourceIndex = (RelevantTextureInfos[idx].y * ORIGINAL_PAGE_SIZE + RelevantTextureInfos[idx].x) * 3;
            int destinationIndex = (RepackedTexturePositions[idx].y * targetTrTexturePanelWidth + RepackedTexturePositions[idx].x) * 3;
            int width = RelevantTextureInfos[idx].width;
            int height = RelevantTextureInfos[idx].height;

            for (int i = 0; i < height; i++)
            {
                if (destinationIndex + height * 3 > atlas24Repacked!.Length 
                    || sourceIndex + height * 3 > atlas24!.Length)
                    continue;
                Array.Copy(atlas24, sourceIndex, atlas24Repacked, destinationIndex, width * 3);
                destinationIndex += targetTrTexturePanelWidth * 3;
                sourceIndex += ORIGINAL_PAGE_SIZE * 3;
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

            const int bytesPerPixel = 3;
            const int blockWidth = 256;
            const int blockHeight = 256;

            int totalPixels = inputData.Length / bytesPerPixel;
            int originalHeight = totalPixels / originalWidth;

            if (originalHeight % blockHeight != 0)
                throw new ArgumentException("Height must be a multiple of 256.");

            int actualBlocks = originalHeight / blockHeight;

            // Calculate rows, rounded
            int blockRows = (int)Math.Ceiling((double)actualBlocks / blocksPerRow);
            int totalBlocks = blockRows * blocksPerRow; // inkl. Padding-Blocks

            int newWidth = blockWidth * blocksPerRow;
            int newHeight = blockHeight * blockRows;

            byte[] outputData = new byte[newWidth * newHeight * bytesPerPixel]; 

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
                        int srcIndex = (srcY * originalWidth + srcX) * bytesPerPixel;

                        int dstX = blockCol * blockWidth + x;
                        int dstY = blockRow * blockHeight + y;
                        int dstIndex = (dstY * newWidth + dstX) * bytesPerPixel;

                        Buffer.BlockCopy(inputData, srcIndex, outputData, dstIndex, bytesPerPixel);
                    }
                }
            }
            return outputData;
        }
    }
}
