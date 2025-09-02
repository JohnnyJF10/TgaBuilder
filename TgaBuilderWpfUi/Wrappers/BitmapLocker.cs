using System;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Runtime.InteropServices;

using TgaBuilderLib.Abstraction;

namespace TgaBuilderWpfUi.Wrappers
{
    /// <summary>
    /// Provides a disposable wrapper for WriteableBitmap pixel buffer access,
    /// allowing safe modification of bitmap data without explicit Lock/Unlock.
    /// </summary>
    public sealed class BitmapLocker : IDisposable, IBitmapLocker
    {
        private readonly WriteableBitmap _bitmap;
        private bool _disposed;
    
        public IntPtr BackBuffer { get; }
        public int Stride { get; }
        public int Width { get; }
        public int Height { get; }
    
        internal BitmapLocker(WriteableBitmap bitmap)
        {
            _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
    
            // Lock bitmap
            _bitmap.Lock();
    
            BackBuffer = _bitmap.BackBuffer;
            Stride = _bitmap.BackBufferStride;
            Width = _bitmap.PixelWidth;
            Height = _bitmap.PixelHeight;
        }
    
        public void Dispose()
        {
            if (_disposed) return;
    
            // Mark changes and unlock
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
            _bitmap.Unlock();
    
            _disposed = true;
        }
    }
}