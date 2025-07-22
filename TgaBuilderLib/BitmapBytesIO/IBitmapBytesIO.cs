using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.FileHandling;

namespace TgaBuilderLib.BitmapBytesIO
{
    public interface IBitmapBytesIO
    {
        ResultStatus ResultInfo { get; }

        byte[]? LoadedBytes { get; }

        int LoadedWidth { get; }
        int LoadedHeight { get; }

        int LoadedStride { get; }
        PixelFormat LoadedFormat { get; }

        int ActualDataLength { get; }

        WriteableBitmap FromOtherBitmap(
            WriteableBitmap source);

        void FromUsual(
            string filePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize);

        void FromPfim(
            string filePath, 
            PixelFormat? targetFormat = null, 
            ResizeMode mode = ResizeMode.SourceResize);

        void FromPsd(
            string filePath, PixelFormat? 
            targetFormat = null, 
            ResizeMode mode = ResizeMode.SourceResize);

        WriteableBitmap GetLoadedBitmap();

        void ToUsual(
            BitmapSource bitmap,
            string extension);

        void WriteUsual(
            string filePath);

        void ToTga(
            BitmapSource bitmap);

        void WriteTga(
            string filePath);

        void ClearLoadedData();
    }
}