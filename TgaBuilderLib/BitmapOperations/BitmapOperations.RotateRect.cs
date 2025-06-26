using System.Windows;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void RotateRec(WriteableBitmap bitmap, Int32Rect rectangle, bool counterclockwise = false)
        {
            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) >> 3;

            int width = rectangle.Width;
            int height = rectangle.Height;

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Rectangle width and height must be greater than zero.");
            }

            var tempBitmap = new WriteableBitmap(height, width, bitmap.DpiX, bitmap.DpiY, bitmap.Format, null);

            int bitmapStride = bitmap.BackBufferStride;
            int tempStride = tempBitmap.BackBufferStride;

            bitmap.Lock();
            tempBitmap.Lock();

            unsafe
            {
                byte* bitmapPtr = (byte*)bitmap.BackBuffer;
                byte* tempPtr = (byte*)tempBitmap.BackBuffer;

                bitmapPtr += rectangle.Y * bitmapStride + rectangle.X * bytesPerPixel;

                if (!counterclockwise)
                {
                    // Rotation clockwise (90°)
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int sourceIndex = y * bitmapStride + x * bytesPerPixel;
                            int destIndex = x * tempStride + (height - y - 1) * bytesPerPixel;
                            for (int b = 0; b < bytesPerPixel; b++)
                            {
                                tempPtr[destIndex + b] = bitmapPtr[sourceIndex + b];
                            }
                        }
                    }
                }
                else
                {
                    // Rotation counterclockwise (270°)
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int sourceIndex = y * bitmapStride + x * bytesPerPixel;
                            int destIndex = (width - x - 1) * tempStride + y * bytesPerPixel;
                            for (int b = 0; b < bytesPerPixel; b++)
                            {
                                tempPtr[destIndex + b] = bitmapPtr[sourceIndex + b];
                            }
                        }
                    }
                }
            }

            bitmap.Unlock();
            tempBitmap.Unlock();

            // Copy the rotated bitmap back to the original bitmap
            FillRectBitmapNoConvert(tempBitmap, bitmap, (rectangle.X, rectangle.Y));
        }
    }
}
