using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Runtime.Intrinsics.X86;
using System.Windows.Media.Media3D;

namespace THelperLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public WriteableBitmap CropBitmap(WriteableBitmap source, Int32Rect rectangle)
        {
            int bytesPerPixel = (source.Format.BitsPerPixel + 7) >> 3;

            int rectX = Math.Max(0, rectangle.X);
            int rectY = Math.Max(0, rectangle.Y);
            int recWidth = Math.Min(rectangle.Width, source.PixelWidth - rectX);
            int recHeight = Math.Min(rectangle.Height, source.PixelHeight - rectY);

            WriteableBitmap target = new WriteableBitmap(recWidth, recHeight, source.DpiX, source.DpiY, source.Format, null);

            int sourceStride = source.BackBufferStride;
            int targetStride = target.BackBufferStride;
            int strideDelta = sourceStride - targetStride;

            source.Lock();
            target.Lock();

            unsafe
            {
                byte* sourcePtr = (byte*)source.BackBuffer;
                byte* targetPtr = (byte*)target.BackBuffer;

                sourcePtr += rectY * sourceStride + rectX * bytesPerPixel;

                for (int r = 0; r < recHeight; r++)
                {
                    for (int s = 0; s < targetStride; s++)
                    {
                        *targetPtr++ = *sourcePtr++;
                    }
                    sourcePtr += strideDelta;
                }
            }
            target.AddDirtyRect(new Int32Rect(0, 0, recWidth, recHeight));
            source.Unlock();
            target.Unlock();

            return target;
        }

        public WriteableBitmap CropBitmap(WriteableBitmap source, Int32Rect rectangle, Color replacedColor, Color newColor)
        {
            if (source.Format != PixelFormats.Rgb24 && source.Format != PixelFormats.Bgra32)
                throw new ArgumentException("Unsupported pixel format. Only Rgb24 and Bgra32 are supported.");

            int rectX = Math.Max(0, rectangle.X);
            int rectY = Math.Max(0, rectangle.Y);
            int recWidth = Math.Min(rectangle.Width, source.PixelWidth - rectX);
            int recHeight = Math.Min(rectangle.Height, source.PixelHeight - rectY);

            WriteableBitmap target = new WriteableBitmap(recWidth, recHeight, source.DpiX, source.DpiY, PixelFormats.Rgb24, null);

            source.Lock();
            target.Lock();

            byte r, g, b, a = 255;
            int sourceBbp = source.Format == PixelFormats.Rgb24 ? 3 : 4;

            unsafe
            {
                byte* srcPtr = (byte*)source.BackBuffer;
                byte* resPtr = (byte*)target.BackBuffer;

                for (int y = 0; y < recHeight; y++)
                {
                    byte* srcRow = srcPtr + (y + rectY) * source.BackBufferStride + sourceBbp * rectX;
                    byte* resRow = resPtr + y * target.BackBufferStride;

                    for (int x = 0; x < recWidth; x++)
                    {
                        if (source.Format == PixelFormats.Bgra32)
                        {
                            b = srcRow[0];
                            g = srcRow[1];
                            r = srcRow[2];
                            a = srcRow[3];
                        }
                        else
                        {
                            r = srcRow[0];
                            g = srcRow[1];
                            b = srcRow[2];
                        }

                        if (a == 0 ||
                            (replacedColor.R == r && replacedColor.G == g && replacedColor.B == b))
                        {
                            // Replace with new color  
                            resRow[0] = newColor.R; // R  
                            resRow[1] = newColor.G; // G  
                            resRow[2] = newColor.B; // B  
                        }
                        else
                        {
                            resRow[0] = r; // R  
                            resRow[1] = g; // G  
                            resRow[2] = b; // B  
                        }

                        srcRow += sourceBbp;
                        resRow += 3; 
                    }
                }
            }

            target.AddDirtyRect(new Int32Rect(0, 0, recWidth, recHeight));
            source.Unlock();
            target.Unlock();

            return target;
        }
    }
}
