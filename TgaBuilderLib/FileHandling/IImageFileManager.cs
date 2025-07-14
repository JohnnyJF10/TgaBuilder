using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        WriteableBitmap OpenImageFile(
            string fileName, 
            PixelFormat? targetFormat = null, 
            ResizeMode mode = ResizeMode.SourceResize);

        void WriteImageFileFromBitmap(
            string fileName,
            BitmapSource bitmap);
    }
}