using System.IO;

public interface IMediaFactory
{
    IWriteableBitmap CreateWriteableBitmap(int width, int height, bool HasAlpha);

    IWriteableBitmap CreateWriteableBitmap(IReadableBitmap source);

    IWriteableBitmap CreateRescaledWriteableBitmap(IWriteableBitmap source, int newWidth, int newHeight);

    IReadableBitmap LoadWriteableBitmap(string filePath);

    IReadableBitmap LoadWriteableBitmap(Stream stream);
}
