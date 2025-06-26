using System.Windows.Media.Imaging;

namespace TgaBuilderLib.FileHandling
{
    public interface ITgaWriter
    {
        void WriteTrTgaFromBytes(string filePath, byte[] data, int width, int height);
        void WriteTrTgaFromBitmap(string filePath, BitmapSource bitmap);
    }
}