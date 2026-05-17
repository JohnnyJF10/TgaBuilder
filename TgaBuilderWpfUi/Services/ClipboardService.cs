using System.Windows;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderWpfUi.Services
{
    internal class ClipboardService : IClipboardService
    {

        public Task SetImageAsync(IReadableBitmap bitmap)
        {
            if (bitmap is not BitmapSourceWrapper wrapper)
                throw new ArgumentException("Bitmap is not a BitmapSource", nameof(bitmap));

            BitmapSource source = wrapper.InnerBitmapSource;

            Clipboard.SetImage(source);

            return Task.CompletedTask;
        }

        public bool ContainsImage()
            => Clipboard.ContainsImage();

        public Task<IReadableBitmap?> GetImageAsync()
        {
            if (Clipboard.GetImage() is not BitmapSource source)
                throw new ArgumentException("Clipboard does not contain a valid image");

            return Task.FromResult<IReadableBitmap?>(new BitmapSourceWrapper(source));
        }
    }
}
