using System.IO;

public interface IMediaFactory
{
    IWriteableBitmap CreateWriteableBitmap(int width, int height, int bitsPerPixel);

    IWriteableBitmap CreateWriteableBitmap(IReadableBitmap source);

    IWriteableBitmap CreateRescaledWriteableBitmap(IWriteableBitmap source, int newWidth, int newHeight);

    IWriteableBitmap LoadWriteableBitmap(string filePath);

    IWriteableBitmap LoadWriteableBitmap(Stream stream);
}
