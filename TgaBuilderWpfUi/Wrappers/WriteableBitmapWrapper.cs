using System;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;

using TgaBuilderLib.Abstraction;
using System.IO;

namespace TgaBuilderWpfUi.Wrappers
{

    public class WriteableBitmapWrapper : IWriteableBitmap
    {
        public WriteableBitmapWrapper(WriteableBitmap bitmap)
        {
            _inner = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        }
        private readonly WriteableBitmap _inner;

        public int PixelWidth => _inner.PixelWidth;
    
        public int PixelHeight => _inner.PixelHeight;
    
        public bool HasAlpha => _inner.Format == PixelFormats.Bgra32 || _inner.Format == PixelFormats.Pbgra32;
    
        public int Size => _inner.PixelWidth * _inner.PixelHeight * (_inner.Format.BitsPerPixel / 8);

        internal WriteableBitmap Inner => _inner;

        public void AddDirtyRect(IntRect dirtyRect)
        {
            var rect = new Int32Rect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);
            _inner.AddDirtyRect(rect);
        }

        public void CopyPixels(IntRect sourceRect, Array pixels, int stride, int offset)
        {
            var rect = new Int32Rect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);
            _inner.CopyPixels(rect, pixels, stride, offset);
        }

        public void CopyPixels(Array pixels, int stride, int offset)
        {
            _inner.CopyPixels(pixels, stride, offset);
        }

        public void Freeze()
        {
            _inner.Freeze();
        }

        public IBitmapLocker GetLocker()
        {
            return new BitmapLocker(_inner);
        }

        public void Lock()
        {
            _inner.Lock();
        }

        public void Save(Stream stream)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(_inner));
            encoder.Save(stream);
        }

        public void Save(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                Save(fs);
            }
        }

        public void Unlock()
        {
            _inner.Unlock();
        }
    }
}