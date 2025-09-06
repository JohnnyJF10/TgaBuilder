using TgaBuilderLib.Abstraction;
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
        bool LoadedHasAlpha { get; }

        int ActualDataLength { get; }

        IWriteableBitmap FromOtherBitmap(
            IWriteableBitmap source);

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

        IWriteableBitmap GetLoadedBitmap();

        void ToUsual(
            IReadableBitmap bitmap,
            string extension);

        void WriteUsual(
            string filePath,
            CancellationToken? cancellationToken = null);

        void ToTga(
            IReadableBitmap bitmap);

        void WriteTga(
            string filePath,
            CancellationToken? cancellationToken = null);

        void ClearLoadedData();
    }
}