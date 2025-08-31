/*

WPF WriteableBitmap
https://learn.microsoft.com/de-de/dotnet/api/system.windows.media.imaging.writeablebitmap?view=windowsdesktop-8.0

Avalonia UI WriteableBitmap
https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_Imaging_WriteableBitmap

*/


using System.Data;
using System.IO;

public interface IWriteableBitmap : IReadableBitmap
{


    public void AddDirtyRect(PixelRect dirtyRect);

    public void CopyPixels(ILockedBitmap targetBitmap);
    public void CopyPixels(PixelRect sourceRect, IntPtr buffer, int bufferSize, int stride);

    //public void Dispose();

    //public bool Equals(object? obj);

    public void Freeze();

    public void Lock();

    public void Unlock();

    public void Save(Stream stream);

    public void Save(string filePath);

}