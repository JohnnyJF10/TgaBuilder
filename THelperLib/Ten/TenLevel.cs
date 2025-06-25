using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace THelperLib.Ten
{
    public partial class TenLevel
    {
        private const int LEGACY_PAGE_SIZE = 256;
        private const int MAX_SUPPORTED_TEX_SIZE = 512;
        private const int ATLAS_MAX_HEIGHT = 32768;

        private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

        private int targetTrTexturePanelWidth;
        private int repackedHeight;

        private List<byte[]> TexPagesList = new();
        private List<(int width, int height, int size)> TexDimsList = new();

        private List<(int page, int x, int y, int width, int height)> RoomsTextureInfos = new();
        private List<(int x, int y)> RepackedTexturePositions = new();

        private byte[]? atlas24Repacked;


        public WriteableBitmap ResultBitmap { get; private set; }

        public bool BitmapSpaceSufficient { get; private set; } = true;


        public TenLevel(string fileName,
            int trTexturePanelHorPagesNum = 2)
        {
            targetTrTexturePanelWidth = trTexturePanelHorPagesNum * LEGACY_PAGE_SIZE;

            ReadLevel(fileName);

            RoomsTextureInfos = RoomsTextureInfos.Distinct().ToList();

            RepackedTexturePositions = RepackAtlas(RoomsTextureInfos.Select(d => (d.width, d.height)).ToList());

            repackedHeight = NextHigherMultiple(repackedHeight, LEGACY_PAGE_SIZE);

            atlas24Repacked = new byte[repackedHeight * targetTrTexturePanelWidth * 4];
            RepackAtlas();

            ResultBitmap = CreateWriteableBitmapFromByteArray(
                byteArray: atlas24Repacked,
                width: targetTrTexturePanelWidth,
                height: repackedHeight,
                pixelFormat: PixelFormats.Bgra32);

            ReturnRentedArrays();
        }

        private void RepackAtlas()
        {
            if (RepackedTexturePositions.Count != RoomsTextureInfos.Count)
                throw new ArgumentException("The number of original and repack tiles must be the same.");

            for (int i = 0; i < RoomsTextureInfos.Count; i++)
            {
                PlaceTile(i);
            }
        }

        public void PlaceTile(int idx)
        {
            int pageIdx = RoomsTextureInfos[idx].page;
            int sourceIndex = (RoomsTextureInfos[idx].y * TexDimsList[pageIdx].width + RoomsTextureInfos[idx].x) * 4;
            int destinationIndex = (RepackedTexturePositions[idx].y * targetTrTexturePanelWidth + RepackedTexturePositions[idx].x) * 4;
            int width = RoomsTextureInfos[idx].width;
            int height = RoomsTextureInfos[idx].height;

            for (int i = 0; i < height; i++)
            {
                if (destinationIndex + height * 3 > atlas24Repacked!.Length) continue;
                if (sourceIndex + height * 3 > TexPagesList[pageIdx].Length) continue;

                Array.Copy(TexPagesList[pageIdx], sourceIndex, atlas24Repacked, destinationIndex, width * 4);
                destinationIndex += targetTrTexturePanelWidth * 4;
                sourceIndex += TexDimsList[pageIdx].width * 4;
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

            // Only visible area
            IntPtr backBuffer = writeableBitmap.BackBuffer;
            Marshal.Copy(byteArray, 0, backBuffer, croppedSize);

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, actualHeight));
            writeableBitmap.Unlock();

            return writeableBitmap;
        }

        private byte[] GetRentedPixelArrayFromPng(byte[] pngBytes, int width, int height)
        {
            var bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream(pngBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; 
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); 
            }

            width = bitmapImage.PixelWidth;
            height = bitmapImage.PixelHeight;

            var wb = new WriteableBitmap(bitmapImage);

            int stride = width * 4; // BGRA32
            byte[] pixelData = _bytePool.Rent(height * stride);

            wb.CopyPixels(pixelData, stride, 0);

            return pixelData; // Format: BGRA (Blue, Green, Red, Alpha)
        }

        private void ReturnRentedArrays()
        {
            foreach (var page in TexPagesList)
            {
                if (page.Length == 0) 
                    continue; 

                _bytePool.Return(page);
            }
        }
    }
}
