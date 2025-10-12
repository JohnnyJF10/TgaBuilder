using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public byte[] GetRegionPixels(IWriteableBitmap bmp, PixelRect rect)
        {
            int stride = rect.Width * (bmp.HasAlpha ? 4 : 3);
            byte[] pixels = new byte[stride * rect.Height];
            bmp.CopyPixels(rect, pixels, stride, 0);
            return pixels;
        }

        public void FillRectArray(IWriteableBitmap bitmap, PixelRect rect, byte[] pixels)
        {
            int bytesPerPixel = bitmap.HasAlpha ? 4 : 3;

            int srcStride = rect.Width * bytesPerPixel;
            int dstStride = bitmap.BackBufferStride;

            using var lockBitmap = bitmap.GetLocker(requiresRefresh: true);

            unsafe
            {
                byte* backBuffer = (byte*)lockBitmap.BackBuffer;
                byte* dstBase = backBuffer + rect.Y * dstStride + rect.X * bytesPerPixel;

                fixed (byte* srcPtr = pixels)
                {
                    byte* srcRow = srcPtr;
                    byte* dstRow = dstBase;

                    for (int y = 0; y < rect.Height; y++)
                    {
                        Buffer.MemoryCopy(srcRow, dstRow, srcStride, srcStride);

                        srcRow += srcStride;
                        dstRow += dstStride;
                    }
                }
            }
        }
    }
}
