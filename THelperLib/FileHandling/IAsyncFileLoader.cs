using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace THelperLib.FileHandling
{
    public interface IAsyncFileLoader
    {
        public HashSet<string> SupportedExtensions { get; }

        int LoadedHeight { get; }
        PixelFormat LoadedPixelFormat { get; }
        int LoadedStride { get; }
        int LoadedWidth { get; }

        Task<WriteableBitmap> LoadAndResizeAsync(string filePath, int targetWidth, int targetHeight, BitmapScalingMode scalingMode);
        byte[] LoadCore(string filePath);
    }
}
