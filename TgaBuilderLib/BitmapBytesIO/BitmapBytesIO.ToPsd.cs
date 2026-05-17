using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Psd;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        public void ToPsd(IReadableBitmap bitmap)
        {
            if (!bitmap.HasAlpha)
                throw new ArgumentException("Bitmap must be in BGRA32 format with an alpha channel.");

            LoadedWidth = bitmap.PixelWidth;
            LoadedHeight = bitmap.PixelHeight;

            ActualDataLength = LoadedWidth * LoadedHeight * 4;
            LoadedBytes = _bytesPool.Rent(ActualDataLength);

            bitmap.CopyPixels(LoadedBytes, LoadedWidth * 4, 0);
        }

        public void WritePsd(string filePath, CancellationToken? cancellationToken = null)
        {
            if (LoadedBytes is null)
                throw new InvalidOperationException("No image data loaded. Please load an image first.");

            var bitmap = _mediaFactory.CreateBitmapFromRaw(
                LoadedWidth, LoadedHeight, hasAlpha: true, LoadedBytes, LoadedWidth * 4);

            var psd = new PsdFile();
            psd.Save(filePath, bitmap, Enumerable.Empty<PsdLayerInfo>());
        }
    }
}
