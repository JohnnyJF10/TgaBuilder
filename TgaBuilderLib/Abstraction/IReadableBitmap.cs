
using System.IO;

public interface IReadableBitmap
{
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public bool HasAlpha { get; }
    public int Size { get; }

    
    void CopyPixels(IntRect sourceRect, Array pixels, int stride, int offset);
    void CopyPixels(Array pixels, int stride, int offset);

    public void Save(Stream stream);
    public void Save(string filePath);
}
