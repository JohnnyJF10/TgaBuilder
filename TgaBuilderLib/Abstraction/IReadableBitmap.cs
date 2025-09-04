
using System.IO;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Abstraction
{
    public interface IReadableBitmap
    {
        public int PixelWidth { get; }
        public int PixelHeight { get; }
        public bool HasAlpha { get; }
        public int Size { get; }
    
        
        void CopyPixels(PixelRect sourceRect, Array pixels, int stride, int offset);
        void CopyPixels(Array pixels, int stride, int offset);
    
        public void Save(Stream stream, EncoderType encoderType = EncoderType.Png);
        public void Save(string filePath, EncoderType encoderType = EncoderType.Png);

        public MemoryStream ToMemoryStream(EncoderType encoderType = EncoderType.Png);
    }
}
