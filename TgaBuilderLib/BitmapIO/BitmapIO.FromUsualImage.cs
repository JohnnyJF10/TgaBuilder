using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapIO
{
    public partial class BitmapIO
    {
        public WriteableBitmap FromUsual(
            string filePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            PixelFormat formatToUse = targetFormat ?? PixelFormats.Rgb24;

            ValidateImageInput(filePath, formatToUse);

            BitmapImage originalImage = GetInputBitmapFromFile(filePath);

            int bytesPerPixel = formatToUse == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = originalImage.PixelWidth;
            int originalHeight = originalImage.PixelHeight;

            int paddedWidth = CalculatePaddedWidth(originalWidth, mode);
            int paddedHeight = CalculatePaddedHeight(originalHeight, mode);

            int stride = paddedWidth * bytesPerPixel;

            FormatConvertedBitmap converted = FormatConvertBitmap(formatToUse, originalImage);

            byte[] blackPixels = CreateBlackPixelBuffer(
                width: paddedWidth,
                height: paddedHeight,
                bytesPerPixel: bytesPerPixel,
                alpha: (byte)(formatToUse == PixelFormats.Bgra32 ? 255 : 0));

            WriteableBitmap paddedBitmap = new WriteableBitmap(
                pixelWidth: paddedWidth,
                pixelHeight: paddedHeight,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: formatToUse,
                palette: null);

            paddedBitmap.WritePixels(
                sourceRect: new System.Windows.Int32Rect(0, 0, paddedWidth, paddedHeight),
                pixels: blackPixels,
                stride: stride,
                offset: 0);

            CroppedBitmap croppedSource = new CroppedBitmap(
                source: converted,
                sourceRect: new System.Windows.Int32Rect(0, 0, originalWidth, originalHeight));

            int srcStride = originalWidth * bytesPerPixel;

            byte[] srcPixels = new byte[originalHeight * srcStride];

            croppedSource.CopyPixels(srcPixels, srcStride, 0);

            paddedBitmap.WritePixels(
                sourceRect: new System.Windows.Int32Rect(0, 0, originalWidth, originalHeight),
                pixels: srcPixels,
                stride: srcStride,
                offset: 0);

            return paddedBitmap;
        }

        private BitmapImage GetInputBitmapFromFile(string filePath)
        {
            BitmapImage originalImage = new BitmapImage();
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                originalImage.BeginInit();
                originalImage.CacheOption = BitmapCacheOption.OnLoad;
                originalImage.StreamSource = stream;
                originalImage.EndInit();
                originalImage.Freeze();
            }

            return originalImage;
        }

        private FormatConvertedBitmap FormatConvertBitmap(PixelFormat formatToUse, BitmapImage originalImage)
        {
            FormatConvertedBitmap converted = new FormatConvertedBitmap();
            converted.BeginInit();
            converted.Source = originalImage;
            converted.DestinationFormat = formatToUse;
            converted.EndInit();
            return converted;
        }
    }
}
