using System.Windows.Media.Imaging;

namespace THelperLib.FileHandling
{
    public interface ITgaWriter
    {
        void WriteTrTgaFromBytes(string filePath, byte[] data, int width, int height);
        void WriteTrTgaFromBitmap(string filePath, BitmapSource bitmap);
    }
}