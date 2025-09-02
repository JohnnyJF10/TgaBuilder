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
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            BitmapSource sourceBitmap = bitmapImage;

            if (sourceBitmap.Format != PixelFormats.Bgra32 && sourceBitmap.Format != PixelFormats.Rgb24)
                throw new NotSupportedException($"Unsupported bitmap format: {sourceBitmap.Format}");

            LoadedHasAlpha = sourceBitmap.Format.BitsPerPixel == 32;

            int originalWidth = sourceBitmap.PixelWidth;
            int originalHeight = sourceBitmap.PixelHeight;

            LoadedWidth = CalculatePaddedWidth(originalWidth, mode);
            LoadedHeight = CalculatePaddedHeight(originalHeight, mode);

            int bytesPerPixel = LoadedHasAlpha ? 4 : 3;
            LoadedStride = LoadedWidth * bytesPerPixel;
            LoadedBytes = RentBlackPixelBuffer(LoadedWidth, LoadedHeight, LoadedHasAlpha);

            sourceBitmap.CopyPixels(LoadedBytes, LoadedStride, 0);
        }
    }
}
