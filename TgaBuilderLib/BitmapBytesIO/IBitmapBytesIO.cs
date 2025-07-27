using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Enums;
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
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null);

        void FromPfim(
            string filePath, 
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null);

        void FromPsd(
            string filePath, 
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null);

        WriteableBitmap GetLoadedBitmap();

        void ToUsual(
            BitmapSource bitmap,
            string extension);

        void WriteUsual(
            string filePath,
            CancellationToken? cancellationToken = null);

        void ToTga(
            BitmapSource bitmap);

        void WriteTga(
            string filePath,
            CancellationToken? cancellationToken = null);

        void ClearLoadedData();
    }
}