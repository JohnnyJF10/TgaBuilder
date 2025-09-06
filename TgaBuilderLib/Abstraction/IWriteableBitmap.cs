/*

WPF IWriteableBitmap
https://learn.microsoft.com/de-de/dotnet/api/system.windows.media.imaging.IWriteableBitmap?view=windowsdesktop-8.0

Avalonia UI IWriteableBitmap
https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_Imaging_IWriteableBitmap

*/


using System.Data;
using System.IO;

namespace TgaBuilderLib.Abstraction
{
    public interface IWriteableBitmap : IReadableBitmap
    {
        public void AddDirtyRect(PixelRect dirtyRect);

        public IBitmapLocker GetLocker(bool requiresRefresh = false);

        public IBitmapLocker GetLocker(PixelRect dirtyRect);

        public void Refresh();

        public void Freeze();
    
        public void WritePixels(PixelRect rect, IntPtr pixels, int stride, int offset = 0);

        public void WritePixels(PixelRect rect, Array pixels, int stride, int offset = 0);

        public int BackBufferStride { get; }
    }
}