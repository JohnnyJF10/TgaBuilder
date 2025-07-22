using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        public WriteableBitmap FromOtherBitmap(WriteableBitmap source)
        {
            if (source.Format != PixelFormats.Bgra32 && source.Format != PixelFormats.Rgb24)
                throw new ArgumentException("Source must be in Bgra32 or Rgb24 format");

            bool isSourceBgra32 = source.Format == PixelFormats.Bgra32;

            int sourceWidth = source.PixelWidth;
            int height = source.PixelHeight;
            int targetWidth = EstimateTargetWidth(sourceWidth);

            WriteableBitmap target = new WriteableBitmap(
                pixelWidth: targetWidth,
                pixelHeight: height,
                dpiX: source.DpiX,
                dpiY: source.DpiY,
                pixelFormat: PixelFormats.Rgb24,
                palette: null);

            source.Lock();
            target.Lock();

            byte r, g, b, a;

            unsafe
            {
                byte* srcPtr = (byte*)source.BackBuffer;
                byte* dstPtr = (byte*)target.BackBuffer;

                int sourceStride = source.BackBufferStride;
                int targetStride = target.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    byte* srcRow = srcPtr + y * sourceStride;
                    byte* dstRow = dstPtr + y * targetStride;

                    for (int x = 0; x < targetWidth; x++)
                    {
                        if (x < sourceWidth)
                        {
                            if (isSourceBgra32)
                            {
                                b = srcRow[x * 4 + 0]; // B
                                g = srcRow[x * 4 + 1]; // G
                                r = srcRow[x * 4 + 2]; // R
                                a = srcRow[x * 4 + 3]; // A
                            }
                            else
                            {
                                r = srcRow[x * 3 + 0]; // R
                                g = srcRow[x * 3 + 1]; // G
                                b = srcRow[x * 3 + 2]; // B
                                a = 255;               // A
                            }

                            if (a != 0)
                            {
                                dstRow[x * 3 + 0] = r;
                                dstRow[x * 3 + 1] = g;
                                dstRow[x * 3 + 2] = b;
                            }
                            else
                            {
                                dstRow[x * 3 + 0] = 255; // R
                                dstRow[x * 3 + 1] = 0;   // G
                                dstRow[x * 3 + 2] = 255; // B
                            }
                        }
                        else
                        {
                            // Magenta 
                            dstRow[x * 3 + 0] = 255; // R
                            dstRow[x * 3 + 1] = 0;   // G
                            dstRow[x * 3 + 2] = 255; // B
                        }
                    }
                }

            }

            target.AddDirtyRect(new Int32Rect(0, 0, targetWidth, height));
            source.Unlock();
            target.Unlock();

            return target;
        }

        private int EstimateTargetWidth(int sourceWidth) => sourceWidth switch
        {
            <= 256 => 256,
            <= 512 => 512,
            <= 1024 => 1024,
            <= 2048 => 2048,
            _ => 4096,
        };

    }
}
