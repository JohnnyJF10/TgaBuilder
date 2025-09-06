
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public Color GetPixelBrush(IWriteableBitmap bitmap, int x, int y)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            x = Math.Clamp(x, 0, bitmap.PixelWidth - 1);
            y = Math.Clamp(y, 0, bitmap.PixelHeight - 1);

            int bytesPerPixel = bitmap.HasAlpha ? 4 : 3;
            int stride = bitmap.BackBufferStride;


            

            byte b, g, r, a;

            using var locker = bitmap.GetLocker();
            {
                unsafe
                {
                    byte* pPixel = (byte*)locker.BackBuffer + (y * stride) + (x * bytesPerPixel);

                    if (bitmap.HasAlpha)
                    {
                        b = pPixel[0];
                        g = pPixel[1];
                        r = pPixel[2];
                        a = pPixel[3];
                    }
                    else
                    {
                        r = pPixel[0];
                        g = pPixel[1];
                        b = pPixel[2];
                        a = 255; // No alpha channel in 24-bit format
                    }
                }
            }
            return new Color(r, g, b, a);
        }
    }
}
