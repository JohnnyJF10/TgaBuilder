
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void FillRectColor(IWriteableBitmap bitmap, PixelRect rect , Color? fillColor = null)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            Color ColorToFill = fillColor ?? new(0,0,0,0);

            int x = Math.Max(0, rect.X);
            int y = Math.Max(0, rect.Y);
            int width = Math.Min(bitmap.PixelWidth - x, rect.Width);
            int height = Math.Min(bitmap.PixelHeight - y, rect.Height);

            if (width <= 0 || height <= 0) 
                return;

            int bytesPerPixel = bitmap.HasAlpha ? 4 : 3;
            int stride = width * bytesPerPixel;
            byte[] pixelData = new byte[stride * height];

            byte[] colorBytes;

            colorBytes = bitmap.HasAlpha 
                ? new byte[] { ColorToFill.B, ColorToFill.G, ColorToFill.R, ColorToFill.A ?? 255 } 
                : new byte[] { ColorToFill.R, ColorToFill.G, ColorToFill.B };

            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
            {
                Array.Copy(colorBytes, 0, pixelData, i, bytesPerPixel);
            }

            bitmap.Lock();
            try
            {
                bitmap.WritePixels(new PixelRect(x, y, width, height), pixelData, stride, 0);
            }
            finally
            {
                bitmap.AddDirtyRect(new PixelRect(x, y, width, height));
                bitmap.Unlock();
            }
        }
    }
}
