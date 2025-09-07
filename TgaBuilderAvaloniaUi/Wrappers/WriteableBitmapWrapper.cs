using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Skia.Lottie;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

using PixelRect = TgaBuilderLib.Abstraction.PixelRect;

namespace TgaBuilderAvaloniaUi.Wrappers
{
    internal class WriteableBitmapWrapper : BitmapWrapper, IWriteableBitmap
    {
        public WriteableBitmapWrapper(Bitmap bitmap) : base(bitmap)
        {
            if (bitmap is WriteableBitmap writeableBitmap)
                _innerWriteableBitmap = writeableBitmap;

            else if (bitmap is Bitmap source)
                _innerWriteableBitmap = GetWriteableBitmap(source);

            else
                throw new ArgumentException("Bitmap is not a WriteableBitmap or BitmapSource", nameof(bitmap));
        }

        public int BackBufferStride => _innerWriteableBitmap.PixelSize.Width * (HasAlpha ? 4 : 3);

        private WriteableBitmap _innerWriteableBitmap;

        public void AddDirtyRect(PixelRect dirtyRect) { /* Nothing to do */ }

        public IBitmapLocker GetLocker(bool requiresRefresh = false)
        {
            var locker = _innerWriteableBitmap.Lock();

            return new BitmapLocker(locker);
        }

        public IBitmapLocker GetLocker(PixelRect dirtyRect) => GetLocker();

        public void Refresh() { /* Nothing to do */ }

        public void Freeze() { /* Nothing to do */ }

        public void WritePixels(PixelRect rect, IntPtr pixels, int stride, int offset = 0)
        {
            using (var fb = _innerWriteableBitmap.Lock())
            {
                int destStride = fb.RowBytes;
                IntPtr destBase = fb.Address;

                for (int y = 0; y < rect.Height; y++)
                {
                    IntPtr srcRow = pixels + offset + y * stride;
                    IntPtr destRow = destBase
                                   + (rect.Y + y) * destStride
                                   + rect.X * 4; // 4 Bytes pro Pixel (Bgra8888)

                    unsafe
                    {
                        Buffer.MemoryCopy(srcRow.ToPointer(),
                                          destRow.ToPointer(),
                                          destStride,
                                          rect.Width * 4);
                    }
                }
            }
        }

        public void WritePixels(PixelRect rect, Array pixels, int stride, int offset = 0)
        {
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));

            if (pixels is not byte[] buffer)
                throw new ArgumentException("Pixels must be a byte[]", nameof(pixels));

            unsafe
            {
                fixed (byte* p = buffer)
                {
                    IntPtr srcPtr = (IntPtr)(p + offset);
                    WritePixels(rect, srcPtr, stride);
                }
            }
        }

        private WriteableBitmap GetWriteableBitmap (Bitmap source)
        {
            using (var ms = new MemoryStream())
            {
                
                source.Save(ms);
                ms.Position = 0;
                
                using (var temp = new Bitmap(ms))
                {
                    var size = temp.Size;
                    var pixelSize = new PixelSize((int)size.Width, (int)size.Height);
                    var dpi = new Vector(96, 96); 

                    var writeable = new WriteableBitmap(
                        pixelSize,
                        dpi,
                        temp.Format,
                        temp.AlphaFormat);

                    var rect = new Avalonia.PixelRect(0, 0, (int)size.Width, (int)size.Height);

                    using (var targetLock = writeable.Lock())
                    {
                        temp.CopyPixels(
                            rect,
                            targetLock.Address, 
                            targetLock.RowBytes * targetLock.Size.Height, 
                            targetLock.RowBytes);
                    }

                    return writeable;
                }
            }
        }
    }
}
