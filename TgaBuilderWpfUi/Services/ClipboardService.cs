using System.Windows;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderWpfUi.Services
{
    internal class ClipboardService : IClipboardService
    {
        private BitmapSource? _currentBitmap;

        public Task SetImageAsync(IReadableBitmap bitmap)
        {
            if (bitmap is not BitmapSourceWrapper wrapper)
                throw new ArgumentException("Bitmap is not a BitmapSource", nameof(bitmap));

            BitmapSource source = wrapper.InnerBitmapSource;

            Clipboard.SetImage(source);

            return Task.CompletedTask;
        }

        public async Task<bool> CheckContainsImageAsync()
        {
            _currentBitmap = Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
            return Task.FromResult(_currentBitmap != null).Result;
        }

        public IReadableBitmap? GetImage()
        {
            if (_currentBitmap is not BitmapSource source)
                throw new ArgumentException("Clipboard does not contain a valid image");

            return new BitmapSourceWrapper(source);
        }
    }
}
