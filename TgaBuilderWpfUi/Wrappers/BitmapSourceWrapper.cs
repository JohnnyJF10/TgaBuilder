using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

public class BitmapSourceWrapper : IReadableBitmap
{
    public BitmapSourceWrapper(BitmapSource bitmap)
    {
        _innerBitmapSource = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
    }
    private readonly BitmapSource _innerBitmapSource;

    internal BitmapSource InnerBitmapSource => _innerBitmapSource;

    public int PixelWidth => _innerBitmapSource.PixelWidth;

    public int PixelHeight => _innerBitmapSource.PixelHeight;

    public bool HasAlpha => _innerBitmapSource.Format == PixelFormats.Bgra32 || _innerBitmapSource.Format == PixelFormats.Pbgra32;

    public int Size => _innerBitmapSource.PixelWidth * _innerBitmapSource.PixelHeight * (_innerBitmapSource.Format.BitsPerPixel / 8);

    public void CopyPixels(PixelRect sourceRect, Array pixels, int stride, int offset)
    {
        var rect = new Int32Rect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);
        _innerBitmapSource.CopyPixels(rect, pixels, stride, offset);
    }

    public void CopyPixels(Array pixels, int stride, int offset)
    {
        _innerBitmapSource.CopyPixels(pixels, stride, offset);
    }

    public void Save(string filePath, EncoderType encoderType = EncoderType.Png)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        Save(fileStream, encoderType);
    }

    public void Save(Stream stream, EncoderType encoderType = EncoderType.Png)
    {
        BitmapEncoder encoder = encoderType switch
        {
            EncoderType.Png     => new PngBitmapEncoder(),
            EncoderType.Jpeg    => new JpegBitmapEncoder(),
            EncoderType.Bmp     => new BmpBitmapEncoder(),
            _ => throw new ArgumentOutOfRangeException(nameof(encoderType), encoderType, null)
        };

        encoder.Frames.Add(BitmapFrame.Create(_innerBitmapSource));
        encoder.Save(stream);
    }

    public MemoryStream ToMemoryStream(EncoderType encoderType = EncoderType.Png)
    {
        var memoryStream = new MemoryStream();
        Save(memoryStream, encoderType);
        memoryStream.Position = 0;
        return memoryStream;
    }
}