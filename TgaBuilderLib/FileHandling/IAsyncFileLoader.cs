
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.FileHandling
{
    public interface IAsyncFileLoader
    {
        public HashSet<string> SupportedExtensions { get; }

        int LoadedHeight { get; }
        bool LoadedHasAlpha { get; }
        int LoadedStride { get; }
        int LoadedWidth { get; }

        //Task<IWriteableBitmap> LoadAndResizeAsync(string filePath, int targetWidth, int targetHeight, BitmapScalingMode scalingMode);

        byte[] LoadCore(string filePath);
    }
}
