using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;

namespace THelperLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void FlipRectVert(WriteableBitmap bitmap, Int32Rect rectangle)
        {
            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) >> 3;

            int width = rectangle.Width;
            int height = rectangle.Height;

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Rectangle width and height must be greater than zero.");
            }

            // Create a temporary bitmap to hold the flipped image
            var tempBitmap = new WriteableBitmap(width, height, bitmap.DpiX, bitmap.DpiY, bitmap.Format, null);

            int bitmapStride = bitmap.BackBufferStride;
            int tempStride = tempBitmap.BackBufferStride;

            bitmap.Lock();
            tempBitmap.Lock();

            unsafe
            {
                byte* bitmapPtr = (byte*)bitmap.BackBuffer;
                byte* tempPtr = (byte*)tempBitmap.BackBuffer;

                // Move the bitmap pointer to the starting position of the rectangle
                bitmapPtr += rectangle.Y * bitmapStride + rectangle.X * bytesPerPixel;

                // Flip the rectangle vertically
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceIndex = y * bitmapStride + x * bytesPerPixel;
                        int tempIndex = (height - y - 1) * tempStride + x * bytesPerPixel;
                        for (int b = 0; b < bytesPerPixel; b++)
                        {
                            tempPtr[tempIndex + b] = bitmapPtr[sourceIndex + b];
                        }
                    }
                }

                bitmap.Unlock();
                tempBitmap.Unlock();

                FillRectBitmapNoConvert(tempBitmap, bitmap, (rectangle.X, rectangle.Y));
            }
        }

    }
}
