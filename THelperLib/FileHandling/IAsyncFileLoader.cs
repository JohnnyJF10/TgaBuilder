using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

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
