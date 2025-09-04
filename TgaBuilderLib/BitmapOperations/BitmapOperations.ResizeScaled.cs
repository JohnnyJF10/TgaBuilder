
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public IWriteableBitmap ResizeScaled(IWriteableBitmap source, int targetWidth, int targetHeight = -1)
        {
            if (targetHeight == -1)
                targetHeight = targetWidth;

            //PixelFormat format = source.Format;

            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;

            int sourceBytesPerPixel = source.HasAlpha ? 4 : 3;
            int targetBytesPerPixel = sourceBytesPerPixel;

            int sourceStride = (sourceWidth * sourceBytesPerPixel + 3) & ~3; 
            int targetStride = (targetWidth * targetBytesPerPixel + 3) & ~3;

            byte[] sourcePixels = new byte[sourceStride * sourceHeight];
            source.CopyPixels(sourcePixels, sourceStride, 0);

            byte[] targetPixels = new byte[targetStride * targetHeight];

            unsafe
            {
                fixed (byte* pSource = sourcePixels)
                fixed (byte* pTarget = targetPixels)
                {
                    byte* src = pSource;
                    byte* dst = pTarget;

                    for (int y = 0; y < targetHeight; y++)
                    {
                        int srcY = y * sourceHeight / targetHeight;
                        byte* srcRow = src + srcY * sourceStride;
                        byte* dstRow = dst + y * targetStride;

                        for (int x = 0; x < targetWidth; x++)
                        {
                            int srcX = x * sourceWidth / targetWidth;
                            byte* srcPixel = srcRow + srcX * sourceBytesPerPixel;

                            if (source.HasAlpha)
                            {
                                // Copy Bgra32 Pixel
                                dstRow[0] = srcPixel[0]; // B
                                dstRow[1] = srcPixel[1]; // G
                                dstRow[2] = srcPixel[2]; // R
                                dstRow[3] = srcPixel[3]; // A

                                dstRow += 4;
                            }
                            else // Rgb24 oder Bgr24
                            {
                                // Copy Rgb24 Pixel
                                dstRow[0] = srcPixel[0]; // B
                                dstRow[1] = srcPixel[1]; // G
                                dstRow[2] = srcPixel[2]; // R

                                dstRow += 3;
                            }
                        }
                    }
                }
            }

            // Output fotmat remains the same as source
            IWriteableBitmap targetBitmap = _mediaFactory.CreateEmptyBitmap(
                width:          targetWidth,
                height:         targetHeight,
                hasAlpha:       source.HasAlpha);

            targetBitmap.WritePixels(new PixelRect(0, 0, targetWidth, targetHeight),
                pixels: targetPixels,
                stride: targetStride,
                offset: 0);

            return targetBitmap;
        }
    }
}
