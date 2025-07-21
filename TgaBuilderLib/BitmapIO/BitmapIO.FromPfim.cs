using Pfim;
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
        public WriteableBitmap FromPfim(
            string filePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            using var stream = File.OpenRead(filePath);
            using var image = Pfimage.FromStream(stream);

            PixelFormat formatToUse = targetFormat ?? PixelFormats.Rgb24;

            ValidateImageInput(filePath, formatToUse);

            int bytesPerPixel = formatToUse == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = image.Width;
            int originalHeight = image.Height;

            int paddedWidth = CalculatePaddedWidth(originalWidth, mode);
            int paddedHeight = CalculatePaddedHeight(originalHeight, mode);

            int paddedStride = paddedWidth * bytesPerPixel;

            byte[] paddedPixels = CreateBlackPixelBuffer(paddedWidth, paddedHeight, bytesPerPixel);

            bool sourceIsRgba = image.Format == ImageFormat.Rgba32;

            for (int y = 0; y < originalHeight; y++)
            {
                int srcOffset = y * image.Stride;
                int dstOffset = y * paddedStride;

                for (int x = 0; x < originalWidth; x++)
                {
                    int srcIndex = srcOffset + x * image.BitsPerPixel / 8;
                    int dstIndex = dstOffset + x * bytesPerPixel;

                    if (formatToUse == PixelFormats.Bgra32 && sourceIsRgba)
                    {
                        paddedPixels[dstIndex + 0] = image.Data[srcIndex + 2]; // B
                        paddedPixels[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                        paddedPixels[dstIndex + 2] = image.Data[srcIndex + 0]; // R
                        paddedPixels[dstIndex + 3] = image.Data[srcIndex + 3]; // A
                    }
                    else if (formatToUse == PixelFormats.Rgb24)
                    {
                        paddedPixels[dstIndex + 0] = image.Data[srcIndex + 2];
                        paddedPixels[dstIndex + 1] = image.Data[srcIndex + 1];
                        paddedPixels[dstIndex + 2] = image.Data[srcIndex + 0];
                    }
                    else
                    {
                        throw new NotSupportedException($"Conversion from {image.Format} to {formatToUse} is not supported.");
                    }
                }
            }

            WriteableBitmap bitmap = new WriteableBitmap(
                pixelWidth: paddedWidth,
                pixelHeight: paddedHeight,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: formatToUse,
                palette: null);

            bitmap.WritePixels(
                sourceRect: new System.Windows.Int32Rect(0, 0, paddedWidth, paddedHeight),
                pixels: paddedPixels,
                stride: paddedStride,
                offset: 0);

            return bitmap;
        }
    }
}
