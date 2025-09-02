

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class BitmapSourceWrapper : IReadableBitmap
{
    public BitmapSourceWrapper(BitmapSource bitmapSource)
    {
        _inner = bitmapSource ?? throw new ArgumentNullException(nameof(bitmapSource));
    }
    private readonly BitmapSource _inner;

    internal BitmapSource Inner => _inner;

    public int PixelWidth => _inner.PixelWidth;

    public int PixelHeight => _inner.PixelHeight;

    public bool HasAlpha => _inner.Format == PixelFormats.Bgra32 || _inner.Format == PixelFormats.Pbgra32;

    public int Size => _inner.PixelWidth * _inner.PixelHeight * (_inner.Format.BitsPerPixel / 8);

    public void CopyPixels(IntRect sourceRect, Array pixels, int stride, int offset)
    {
        var rect = new Int32Rect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);
        _inner.CopyPixels(rect, pixels, stride, offset);
    }

    public void CopyPixels(Array pixels, int stride, int offset)
    {
        _inner.CopyPixels(pixels, stride, offset);
    }

    public void Save(Stream stream)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_inner));
        encoder.Save(stream);
    }

    public void Save(string filePath)
    {
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            Save(stream);
        }
    }
}