using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void FillRectColor(WriteableBitmap bitmap, Int32Rect rect , Color? fillColor = null)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            Color ColorToFill = fillColor ?? Color.FromArgb(0,0,0,0);

            int x = Math.Max(0, rect.X);
            int y = Math.Max(0, rect.Y);
            int width = Math.Min(bitmap.PixelWidth - x, rect.Width);
            int height = Math.Min(bitmap.PixelHeight - y, rect.Height);

            if (width <= 0 || height <= 0) return;

            PixelFormat format = bitmap.Format;
            int bytesPerPixel = (format.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            byte[] pixelData = new byte[stride * height];

            byte[] colorBytes;

            colorBytes = format switch
            {
                PixelFormat f when f == PixelFormats.Rgb24 
                    => new byte[] {ColorToFill.R, ColorToFill.G, ColorToFill.B },

                PixelFormat f when f == PixelFormats.Bgra32
                    => new byte[] { ColorToFill.B, ColorToFill.G, ColorToFill.R, ColorToFill.A },

                _ => throw new NotSupportedException($"Unsupported pixel format: {format}"),
            };

            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
            {
                Array.Copy(colorBytes, 0, pixelData, i, bytesPerPixel);
            }

            bitmap.Lock();
            try
            {
                bitmap.WritePixels(new Int32Rect(x, y, width, height), pixelData, stride, 0);
            }
            finally
            {
                bitmap.AddDirtyRect(new Int32Rect(x, y, width, height));
                bitmap.Unlock();
            }
        }
    }
}
