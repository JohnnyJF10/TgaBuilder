using System;
using System.Collections.Generic;
using System.Drawing.PSD;
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
        public void FromPsd(
            string psdFilePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize)
        {
            LoadedFormat = targetFormat ?? PixelFormats.Rgb24;

            ValidateImageInput(psdFilePath, LoadedFormat);

            var psd = new PsdFile();
            psd.Load(psdFilePath);

            int bytesPerPixel = LoadedFormat == PixelFormats.Bgra32 ? 4 : 3;

            int originalWidth = psd.Columns;
            int originalHeight = psd.Rows;

            LoadedWidth = CalculatePaddedWidth(originalWidth, mode);
            LoadedHeight = CalculatePaddedHeight(originalHeight, mode);

            LoadedStride = LoadedWidth * bytesPerPixel;

            LoadedBytes = RentBlackPixelBuffer(
                width: LoadedWidth,
                height: LoadedHeight,
                bytesPerPixel: bytesPerPixel);

            var data = psd.ImageData;
            int channelCount = data.Length;

            for (int y = 0; y < originalHeight; y++)
            {
                for (int x = 0; x < originalWidth; x++)
                {
                    int layerIndex = y * originalWidth + x;
                    int globalIndex = (y * LoadedWidth + x) * bytesPerPixel;

                    byte a = channelCount == 4 ? data[3][layerIndex] : (byte)255;
                    byte r = data[0][layerIndex];
                    byte g = data[1][layerIndex];
                    byte b = data[2][layerIndex];

                    if (LoadedFormat == PixelFormats.Bgra32)
                    {
                        LoadedBytes[globalIndex + 0] = b;
                        LoadedBytes[globalIndex + 1] = g;
                        LoadedBytes[globalIndex + 2] = r;
                        LoadedBytes[globalIndex + 3] = a;
                    }
                    else if (LoadedFormat == PixelFormats.Rgb24)
                    {
                        LoadedBytes[globalIndex + 0] = r;
                        LoadedBytes[globalIndex + 1] = g;
                        LoadedBytes[globalIndex + 2] = b;
                    }
                }
            }

            //WriteableBitmap wb = new WriteableBitmap(
            //    pixelWidth: LoadedWidth,
            //    pixelHeight: LoadedHeight,
            //    dpiX: 96,
            //    dpiY: 96,
            //    pixelFormat: LoadedFormat,
            //    palette: null);
            //
            //wb.WritePixels(
            //    sourceRect: new System.Windows.Int32Rect(0, 0, LoadedWidth, LoadedHeight),
            //    pixels: LoadedBytes,
            //    stride: LoadedStride,
            //    offset: 0);
            //
            //return wb;
        }
    }
}
