using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void FlipRectHor(IWriteableBitmap bitmap, PixelRect rectangle)
        {
            int bytesPerPixel = bitmap.HasAlpha ? 4 : 3;

            int width = rectangle.Width;
            int height = rectangle.Height;

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Rectangle width and height must be greater than zero.");
            }

            // Create a temporary bitmap to hold the flipped image
            var tempBitmap = _mediaFactory.CreateEmptyBitmap(
                width:          width, 
                height:         height, 
                hasAlpha:       bytesPerPixel == 4);

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

                // Flip the rectangle horizontally
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceIndex = y * bitmapStride + x * bytesPerPixel;
                        int tempIndex = y * tempStride + (width - x - 1) * bytesPerPixel;
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
