using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void RotateRec(IWriteableBitmap bitmap, PixelRect rectangle, bool counterclockwise = false)
        {
            int bytesPerPixel = bitmap.HasAlpha ? 4 : 3;

            int width = rectangle.Width;
            int height = rectangle.Height;

            if (width <= 0 || height <= 0)
                throw new ArgumentException("Rectangle width and height must be greater than zero.");

            IWriteableBitmap tempBitmap = _mediaFactory.CreateEmptyBitmap(
                width:          width, 
                height:         height, 
                hasAlpha:       bytesPerPixel == 4);

            int bitmapStride = bitmap.BackBufferStride;
            int tempStride = tempBitmap.BackBufferStride;

            using var bitmapLocker = bitmap.GetLocker();
            using var tempLocker = tempBitmap.GetLocker();
            {
                unsafe
                {
                    byte* bitmapPtr = (byte*)bitmapLocker.BackBuffer;
                    byte* tempPtr = (byte*)tempLocker.BackBuffer;

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
            } // End using lockers

            // Copy the rotated bitmap back to the original bitmap
            FillRectBitmapNoConvert(tempBitmap, bitmap, (rectangle.X, rectangle.Y));
        }
    }
}
