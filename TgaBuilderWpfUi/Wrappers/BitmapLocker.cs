using System;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Runtime.InteropServices;

using TgaBuilderLib.Abstraction;
using System.Diagnostics;

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

        public Int32Rect DirtyRect { get; }

        public bool RequiresRefresh { get; }


        internal BitmapLocker(WriteableBitmap bitmap, bool requiresRefresh = true, Int32Rect? dirtyRect = null)
        {
            _bitmap = bitmap;

            DirtyRect = dirtyRect ?? new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);

            if (DirtyRect.X < 0 || DirtyRect.Y < 0 || DirtyRect.Width <= 0 || DirtyRect.Height <= 0 ||
                DirtyRect.X + DirtyRect.Width > bitmap.PixelWidth ||
                DirtyRect.Y + DirtyRect.Height > bitmap.PixelHeight)
            {
                Debug.WriteLine(false, "CAUTION: DirtyRect is out of bounds. Fallback to full bitmap."+
                    $"Violated Conditions: " + 
                    $"DirtyRect.X < 0: {DirtyRect.X < 0}, " +
                    $"DirtyRect.Y < 0: {DirtyRect.Y < 0}, " +
                    $"DirtyRect.Width <= 0: {DirtyRect.Width <= 0}, " +
                    $"DirtyRect.Height <= 0: {DirtyRect.Height <= 0}, " +
                    $"DirtyRect.X + DirtyRect.Width > bitmap.PixelWidth: {DirtyRect.X + DirtyRect.Width > bitmap.PixelWidth}, " +
                    $"DirtyRect.Y + DirtyRect.Height > bitmap.PixelHeight: {DirtyRect.Y + DirtyRect.Height > bitmap.PixelHeight}"
                    );
                DirtyRect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
            }

            RequiresRefresh = requiresRefresh;

            _bitmap.Lock();
    
            BackBuffer = _bitmap.BackBuffer;
        }
    
        public void Dispose()
        {
            if (_disposed) 
                return;

            if (RequiresRefresh)
                _bitmap.AddDirtyRect(DirtyRect);

            _bitmap.Unlock();
    
            _disposed = true;
        }
    }
}