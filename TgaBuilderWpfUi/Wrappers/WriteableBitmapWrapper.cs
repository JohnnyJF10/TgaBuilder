using System;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;

using TgaBuilderLib.Abstraction;
using System.IO;
using TgaBuilderLib.Enums;

namespace TgaBuilderWpfUi.Wrappers
{

    public class WriteableBitmapWrapper : BitmapSourceWrapper, IWriteableBitmap
    {
        private readonly WriteableBitmap _innerWriteableBitmap;

        public WriteableBitmapWrapper(BitmapSource bitmap) : base(bitmap)
        {
            if (bitmap is WriteableBitmap writeableBitmap)
                _innerWriteableBitmap = writeableBitmap;
            else if (bitmap is BitmapSource source)
                _innerWriteableBitmap = new WriteableBitmap(source);
            else
                throw new ArgumentException("Bitmap is not a WriteableBitmap or BitmapSource", nameof(bitmap));
        }

        internal WriteableBitmap InnerWriteableBitmap => _innerWriteableBitmap;

        public IntPtr BackBuffer => InnerWriteableBitmap.BackBuffer;

        public int BackBufferStride => InnerWriteableBitmap.BackBufferStride;

        public void AddDirtyRect(PixelRect dirtyRect)
        {
            var rect = new Int32Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);
            _innerWriteableBitmap.AddDirtyRect(rect);
        }

        public void Freeze()
        {
            _innerWriteableBitmap.Freeze();
        }

        public IBitmapLocker GetLocker(bool requiresRefresh = false)
        {
            Int32Rect? rect = null;

            if (requiresRefresh)
                rect = new Int32Rect(0, 0, 
                    _innerWriteableBitmap.PixelWidth, _innerWriteableBitmap.PixelHeight);

            return new BitmapLocker(_innerWriteableBitmap, requiresRefresh, rect);
        }

        public IBitmapLocker GetLocker(PixelRect dirtyRect)
        {
            Int32Rect rect = new Int32Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

            return new BitmapLocker(_innerWriteableBitmap, true, rect);
        }

        public void Lock()
        {
            _innerWriteableBitmap.Lock();
        }

        public void Unlock()
        {
            _innerWriteableBitmap.Unlock();
        }

        public void WritePixels(PixelRect rect, IntPtr pixels, int stride, int offset = 0)
        {
            var intRect = new Int32Rect(rect.X, rect.Y, rect.Width, rect.Height);
            _innerWriteableBitmap.WritePixels(intRect, pixels, stride, offset);
        }

        public void WritePixels(PixelRect rect, Array pixels, int stride, int offset = 0)
        {
            var intRect = new Int32Rect(rect.X, rect.Y, rect.Width, rect.Height);
            _innerWriteableBitmap.WritePixels(intRect, pixels, stride, offset);
        }

        public void Refresh()
        {
            Int32Rect rect = new Int32Rect(0, 0, _innerWriteableBitmap.PixelWidth, _innerWriteableBitmap.PixelHeight);

            _innerWriteableBitmap.Lock();
            _innerWriteableBitmap.AddDirtyRect(rect);
            _innerWriteableBitmap.Unlock();
        }
    }
}