using System.Windows;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderWpfUi.Services
{
    internal class ClipboardService : IClipboardService
    {

        public void SetImage(IReadableBitmap bitmap)
        {
            if (bitmap is not BitmapSourceWrapper wrapper)
                throw new ArgumentException("Bitmap is not a BitmapSource", nameof(bitmap));

            BitmapSource source = wrapper.InnerBitmapSource;

            Clipboard.SetImage(source);
        }

        public bool ContainsImage()
            => Clipboard.ContainsImage();

        public IReadableBitmap? GetImage()
        {
            if (Clipboard.GetImage() is not BitmapSource source)
                throw new ArgumentException("Clipboard does not contain a valid image");

            return new BitmapSourceWrapper(source);
        }
    }
}
