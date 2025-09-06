using System.IO;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Abstraction
{
    public interface IMediaFactory
    {
        IWriteableBitmap CreateEmptyBitmap(int width, int height, bool hasAlpha);
    
        IWriteableBitmap CloneBitmap(IReadableBitmap source);
    
        IWriteableBitmap CreateRescaledBitmap(IWriteableBitmap source, int newWidth, int newHeight, BitmapScalingMode scalingMode = BitmapScalingMode.Linear);

        IWriteableBitmap LoadBitmap(string filePath);

        IWriteableBitmap LoadBitmap(Stream stream);


        IReadableBitmap CreateBitmapFromRaw(int pixelWidth, int pixelHeight, bool hasAlpha, Array pixels, int stride);

        IReadableBitmap LoadReadableBitmap(string filePath);
    
        IReadableBitmap LoadReadableBitmap(Stream stream);
    }
}