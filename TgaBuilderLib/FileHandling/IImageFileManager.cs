using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.FileHandling
{
    public enum ResultStatus
    {
        Success,
        RezisingRequired,
        BitmapAreaNotSufficient,
    }

    public interface IImageFileManager
    {
        public ResultStatus ResultInfo { get; }

        public bool TrImportRepackingSelected { get; set; }
        public int TrImportHorPageNum { get; set; }

        void LoadImageFile(
            string fileName, 
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null);

        IWriteableBitmap GetLoadedBitmap();

        IWriteableBitmap GetDestinationConfirmBitmap(
            IWriteableBitmap inputBitmap);

        void SaveImageFile(
            string fileName,
            IReadableBitmap bitmap);

        void WriteImageFile(
            string fileName,
            CancellationToken? cancellationToken = null);

        void ClearLoadedData();
    }
}