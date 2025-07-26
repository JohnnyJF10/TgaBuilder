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

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        public void FromPfim(
            string filePath,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            using var stream = File.OpenRead(filePath);
            using var image = Pfimage.FromStream(stream);

            bool isPfimRgba32 = image.Format == ImageFormat.Rgba32;

            LoadedFormat = isPfimRgba32 ? PixelFormats.Bgra32 : PixelFormats.Rgb24;

            ValidateImageInput(filePath, LoadedFormat);

            int bytesPerPixel = LoadedFormat == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = image.Width;
            int originalHeight = image.Height;

            LoadedWidth = CalculatePaddedWidth(originalWidth, mode);
            LoadedHeight = CalculatePaddedHeight(originalHeight, mode);

            LoadedStride = LoadedWidth * bytesPerPixel;

            LoadedBytes = RentBlackPixelBuffer(LoadedWidth, LoadedHeight, bytesPerPixel);

            for (int y = 0; y < originalHeight; y++)
            {
                int srcOffset = y * image.Stride;
                int dstOffset = y * LoadedStride;

                for (int x = 0; x < originalWidth; x++)
                {
                    int srcIndex = srcOffset + x * image.BitsPerPixel / 8;
                    int dstIndex = dstOffset + x * bytesPerPixel;

                    if (LoadedFormat == PixelFormats.Bgra32 && isPfimRgba32)
                    {
                        LoadedBytes[dstIndex + 0] = image.Data[srcIndex + 2]; // B
                        LoadedBytes[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                        LoadedBytes[dstIndex + 2] = image.Data[srcIndex + 0]; // R
                        LoadedBytes[dstIndex + 3] = image.Data[srcIndex + 3]; // A
                    }
                    else if (LoadedFormat == PixelFormats.Rgb24)
                    {
                        LoadedBytes[dstIndex + 0] = image.Data[srcIndex + 2];
                        LoadedBytes[dstIndex + 1] = image.Data[srcIndex + 1];
                        LoadedBytes[dstIndex + 2] = image.Data[srcIndex + 0];
                    }
                    else
                    {
                        throw new NotSupportedException($"Conversion from {image.Format} to {LoadedFormat} is not supported.");
                    }
                }
            }

            //WriteableBitmap bitmap = new WriteableBitmap(
            //    pixelWidth: LoadedWidth,
            //    pixelHeight: LoadedHeight,
            //    dpiX: 96,
            //    dpiY: 96,
            //    pixelFormat: LoadedFormat,
            //    palette: null);
            //
            //bitmap.WritePixels(
            //    sourceRect: new System.Windows.Int32Rect(0, 0, LoadedWidth, LoadedHeight),
            //    pixels: LoadedBytes,
            //    stride: LoadedStride,
            //    offset: 0);
            //
            //return bitmap;
        }
    }
}
