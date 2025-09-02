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


    public void AddDirtyRect(IntRect dirtyRect);

    IBitmapLocker GetLocker();

    public void Freeze();

    public void Lock();

    public void Unlock();

}