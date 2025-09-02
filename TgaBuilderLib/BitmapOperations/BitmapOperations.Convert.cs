using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public WriteableBitmap ConvertRGB24ToBGRA32(WriteableBitmap sourceBitmap)
        {
            if (sourceBitmap == null)
                throw new ArgumentNullException(nameof(sourceBitmap), "Source bitmap cannot be null.");

            if (sourceBitmap.Format.BitsPerPixel != 24)
                throw new ArgumentException("Source bitmap must be in RGB24 format.", nameof(sourceBitmap));

            // Create a new WriteableBitmap with BGRA32 format
            var targetBitmap = GetNewWriteableBitmap(
                width:          sourceBitmap.PixelWidth,
                height:         sourceBitmap.PixelHeight,
                hasAlpha:       true);

            // Lock the source and target bitmaps for writing
            sourceBitmap.Lock();
            targetBitmap.Lock();
            unsafe
            {
                byte* srcPtr = (byte*)sourceBitmap.BackBuffer;
                byte* dstPtr = (byte*)targetBitmap.BackBuffer;

                int width = sourceBitmap.PixelWidth;
                int height = sourceBitmap.PixelHeight;
                int srcStride = sourceBitmap.BackBufferStride;
                int dstStride = targetBitmap.BackBufferStride;

                byte r, g, b;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Read RGB values from the source bitmap
                        r = srcPtr[y * srcStride + x * 3];
                        g = srcPtr[y * srcStride + x * 3 + 1];
                        b = srcPtr[y * srcStride + x * 3 + 2];

                        if ((r, g, b) != (255, 0, 255)) // Write BGRA values to the target bitmap if not magenta
                        {
                            dstPtr[y * dstStride + x * 4    ] = b;   // B
                            dstPtr[y * dstStride + x * 4 + 1] = g;   // G
                            dstPtr[y * dstStride + x * 4 + 2] = r;   // R
                            dstPtr[y * dstStride + x * 4 + 3] = 255; // A (fully opaque)
                        }
                    }
                }
            }

            // Update the target bitmap's back buffer
            targetBitmap.AddDirtyRect(new Int32Rect(0, 0, targetBitmap.PixelWidth, targetBitmap.PixelHeight));
            targetBitmap.Unlock();
            sourceBitmap.Unlock();

            return targetBitmap;
        }

        public WriteableBitmap ConvertBGRA32ToRGB24(WriteableBitmap sourceBitmap)
        {
            if (sourceBitmap == null)
                throw new ArgumentNullException(nameof(sourceBitmap), "Source bitmap cannot be null.");
            if (sourceBitmap.Format.BitsPerPixel != 32)
                throw new ArgumentException("Source bitmap must be in BGRA32 format.", nameof(sourceBitmap));
            
            // Create a new WriteableBitmap with RGB24 format
            var targetBitmap = GetNewWriteableBitmap(
                width:          sourceBitmap.PixelWidth,
                height:         sourceBitmap.PixelHeight,
                hasAlpha:       false);

            // Lock the source and target bitmaps for writing
            sourceBitmap.Lock();
            targetBitmap.Lock();
            unsafe
            {
                byte* srcPtr = (byte*)sourceBitmap.BackBuffer;
                byte* dstPtr = (byte*)targetBitmap.BackBuffer;

                int width = sourceBitmap.PixelWidth;
                int height = sourceBitmap.PixelHeight;
                int srcStride = sourceBitmap.BackBufferStride;
                int dstStride = targetBitmap.BackBufferStride;

                byte r, g, b, a;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Read BGRA values from the source bitmap
                        b = srcPtr[y * srcStride + x * 4];
                        g = srcPtr[y * srcStride + x * 4 + 1];
                        r = srcPtr[y * srcStride + x * 4 + 2];
                        a = srcPtr[y * srcStride + x * 4 + 3];

                        if (a == 0) // If fully transparent, write magegenta
                        {
                            dstPtr[y * dstStride + x * 3    ] = 255;   // R
                            dstPtr[y * dstStride + x * 3 + 1] = 0;     // G
                            dstPtr[y * dstStride + x * 3 + 2] = 255;   // B
                        }
                        else if (a < 255) // If semi-transparent, blend with black
                        {
                            dstPtr[y * dstStride + x * 3    ] = (byte)(r * a / 255);   // R
                            dstPtr[y * dstStride + x * 3 + 1] = (byte)(g * a / 255);   // G
                            dstPtr[y * dstStride + x * 3 + 2] = (byte)(b * a / 255);   // B
                        }
                        else // Write RGB values to the target bitmap
                        {
                            dstPtr[y * dstStride + x * 3    ] = r;   // R
                            dstPtr[y * dstStride + x * 3 + 1] = g;   // G
                            dstPtr[y * dstStride + x * 3 + 2] = b;   // B                        
                        }
                    }
                }
                // Update the target bitmap's back buffer
                targetBitmap.AddDirtyRect(new Int32Rect(0, 0, targetBitmap.PixelWidth, targetBitmap.PixelHeight));
                targetBitmap.Unlock();
                sourceBitmap.Unlock();
                return targetBitmap;
            }
        }
    }
}