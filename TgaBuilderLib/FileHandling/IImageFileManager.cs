using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

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
            PixelFormat? targetFormat = null, 
            ResizeMode mode = ResizeMode.SourceResize);

        WriteableBitmap GetLoadedBitmap();

        WriteableBitmap GetDestinationConfirmBitmap(
            WriteableBitmap inputBitmap);

        void SaveImageFile(
            string fileName,
            BitmapSource bitmap);

        void WriteImageFile(
            string fileName);

        void ClearLoadedData();
    }
}