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
            int stride = rect.Width * (bitmap.HasAlpha ? 4 : 3);
            bitmap.WritePixels(rect, pixels, stride, 0);
            bitmap.Refresh();
        }
    }
}
