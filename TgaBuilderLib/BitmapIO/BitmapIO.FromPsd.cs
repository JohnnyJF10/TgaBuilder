using System;
using System.Collections.Generic;
using System.Drawing.PSD;
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
        public WriteableBitmap FromPsd(
            string psdFilePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            PixelFormat formatToUse = targetFormat ?? PixelFormats.Rgb24;

            ValidateImageInput(psdFilePath, formatToUse);

            var psd = new PsdFile();
            psd.Load(psdFilePath);

            int bytesPerPixel = formatToUse == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = psd.Columns;
            int originalHeight = psd.Rows;

            int paddedWidth = CalculatePaddedWidth(originalWidth, mode);
            int paddedHeight = CalculatePaddedHeight(originalHeight, mode);

            int paddedStride = paddedWidth * bytesPerPixel;

            byte[] paddedPixels = CreateBlackPixelBuffer(
                width: paddedWidth,
                height: paddedHeight,
                bytesPerPixel: bytesPerPixel);

            var data = psd.ImageData;
            int channelCount = data.Length;

            for (int y = 0; y < originalHeight; y++)
            {
                for (int x = 0; x < originalWidth; x++)
                {
                    int layerIndex = y * originalWidth + x;
                    int globalIndex = (y * paddedWidth + x) * bytesPerPixel;

                    byte a = channelCount == 4 ? data[3][layerIndex] : (byte)255;
                    byte r = data[0][layerIndex];
                    byte g = data[1][layerIndex];
                    byte b = data[2][layerIndex];

                    if (formatToUse == PixelFormats.Bgra32)
                    {
                        paddedPixels[globalIndex + 0] = b;
                        paddedPixels[globalIndex + 1] = g;
                        paddedPixels[globalIndex + 2] = r;
                        paddedPixels[globalIndex + 3] = a;
                    }
                    else if (formatToUse == PixelFormats.Rgb24)
                    {
                        paddedPixels[globalIndex + 0] = r;
                        paddedPixels[globalIndex + 1] = g;
                        paddedPixels[globalIndex + 2] = b;
                    }
                }
            }

            WriteableBitmap wb = new WriteableBitmap(
                pixelWidth: paddedWidth,
                pixelHeight: paddedHeight,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: formatToUse,
                palette: null);

            wb.WritePixels(
                sourceRect: new System.Windows.Int32Rect(0, 0, paddedWidth, paddedHeight),
                pixels: paddedPixels,
                stride: paddedStride,
                offset: 0);

            return wb;
        }
    }
}
