using Pfim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        public void FromUsual(
            string filePath,
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            LoadedFormat = bitmapImage.Format.BitsPerPixel == 32
                ? PixelFormats.Bgra32
                : PixelFormats.Rgb24;
            ValidateImageInput(filePath, LoadedFormat);

            int originalWidth = bitmapImage.PixelWidth;
            int originalHeight = bitmapImage.PixelHeight;

            LoadedWidth = CalculatePaddedWidth(originalWidth, mode);
            LoadedHeight = CalculatePaddedHeight(originalHeight, mode);

            int bytesPerPixel = (LoadedFormat == PixelFormats.Bgra32) ? 4 : 3;
            LoadedStride = LoadedWidth * bytesPerPixel;
            LoadedBytes = RentBlackPixelBuffer(LoadedWidth, LoadedHeight, bytesPerPixel);

            FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap(bitmapImage, LoadedFormat, null, 0);
            int stride = formattedBitmap.PixelWidth * (formattedBitmap.Format.BitsPerPixel + 7) / 8;
            byte[] rawBytes = new byte[stride * formattedBitmap.PixelHeight];
            formattedBitmap.CopyPixels(rawBytes, stride, 0);

            for (int y = 0; y < originalHeight; y++)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                int srcOffset = y * stride;
                int dstOffset = y * LoadedStride;

                Buffer.BlockCopy(rawBytes, srcOffset, LoadedBytes, dstOffset, originalWidth * bytesPerPixel);
            }
        }
    }
}
