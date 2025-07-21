using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.FileHandling;

namespace TgaBuilderLib.BitmapIO
{
    public interface IBitmapIO
    {
        ResultStatus ResultInfo { get; }

        WriteableBitmap FromOtherBitmap(
            WriteableBitmap source);

        WriteableBitmap FromUsual(
            string filePath,
            PixelFormat? targetFormat = null,
            ResizeMode mode = ResizeMode.SourceResize);

        WriteableBitmap FromPfim(
            string filePath, 
            PixelFormat? targetFormat = null, 
            ResizeMode mode = ResizeMode.SourceResize);

        WriteableBitmap FromPsd(
            string filePath, PixelFormat? 
            targetFormat = null, 
            ResizeMode mode = ResizeMode.SourceResize);

        void ToUsual(
            string filePath, 
            string extension,
            BitmapSource bitmap);

        void ToTga(
            string filePath, 
            BitmapSource bitmap);
    }
}