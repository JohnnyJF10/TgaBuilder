using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace THelperLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public Color GetPixelBrush(WriteableBitmap bitmap, int x, int y)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            x = Math.Clamp(x, 0, bitmap.PixelWidth - 1);
            y = Math.Clamp(y, 0, bitmap.PixelHeight - 1);

            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
            int stride = bitmap.BackBufferStride;
            IntPtr pBackBuffer = bitmap.BackBuffer;

            unsafe
            {
                byte* pPixel = (byte*)pBackBuffer.ToPointer() + y * stride + x * bytesPerPixel;

                byte b = pPixel[2];
                byte g = pPixel[1];
                byte r = pPixel[0];
                byte a = (bytesPerPixel >= 4) ? pPixel[3] : (byte)255;

                return Color.FromArgb(a, r, g, b);
            }
        }
    }
}
