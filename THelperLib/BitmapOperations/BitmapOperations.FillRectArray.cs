using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using THelperLib.UndoRedo;

namespace THelperLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public byte[] GetRegionPixels(WriteableBitmap bmp, Int32Rect rect)
        {
            int stride = rect.Width * (bmp.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[stride * rect.Height];
            bmp.CopyPixels(rect, pixels, stride, 0);
            return pixels;
        }

        public void FillRectArray(WriteableBitmap bitmap, Int32Rect rect, byte[] pixels)
        {
            int stride = rect.Width * (bitmap.Format.BitsPerPixel / 8);
            bitmap.WritePixels(rect, pixels, stride, 0);
            bitmap.Lock();
            bitmap.AddDirtyRect(rect);
            bitmap.Unlock();
        }
    }
}
