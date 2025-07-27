using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        WriteableBitmap GetLoadedBitmap();

        WriteableBitmap GetDestinationConfirmBitmap(
            WriteableBitmap inputBitmap);

        void SaveImageFile(
            string fileName,
            BitmapSource bitmap);

        void WriteImageFile(
            string fileName,
            CancellationToken? cancellationToken = null);

        void ClearLoadedData();
    }
}