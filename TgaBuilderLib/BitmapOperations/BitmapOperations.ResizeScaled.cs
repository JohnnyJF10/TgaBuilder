using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public WriteableBitmap ResizeScaled(WriteableBitmap source, int targetWidth, int targetHeight = -1)
        {
            if (targetHeight == -1)
                targetHeight = targetWidth;

            PixelFormat format = source.Format;

            if (format != PixelFormats.Bgr24 && format != PixelFormats.Rgb24 && format != PixelFormats.Bgra32)
                throw new NotSupportedException("Only Bgr24, Rgb24 and Bgra32 formats are supported.");

            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;

            int sourceBytesPerPixel = format.BitsPerPixel / 8;
            int targetBytesPerPixel = (format == PixelFormats.Bgra32) ? 4 : 3; 

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

                            if (format == PixelFormats.Bgra32)
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
            var targetBitmap = new WriteableBitmap(
                targetWidth,
                targetHeight,
                96, 96,
                format,
                null);

            targetBitmap.WritePixels(new Int32Rect(0, 0, targetWidth, targetHeight), targetPixels, targetStride, 0);

            return targetBitmap;
        }
    }
}
