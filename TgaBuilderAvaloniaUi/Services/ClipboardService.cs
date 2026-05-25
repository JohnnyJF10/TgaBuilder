using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using System;
using System.Threading.Tasks;
using TgaBuilderAvaloniaUi.View;
using TgaBuilderAvaloniaUi.Wrappers;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class ClipboardService : IClipboardService
    {
        IClipboard? _clipboard;
        Bitmap? _currentBitmap;

        public async Task<bool> CheckContainsImageAsync()
        {
            Bitmap? bitmap = await (_clipboard?.TryGetBitmapAsync() ?? Task.FromResult<Bitmap?>(null));
            _currentBitmap = bitmap;
            return bitmap != null;
        }

        public IReadableBitmap? GetImage() 
            => _currentBitmap != null ? new BitmapWrapper(_currentBitmap) : null;

        public async Task SetImageAsync(IReadableBitmap bitmap)
        {
            if (_clipboard is null) 
                throw new InvalidOperationException("Clipboard service is not initialized.");

            await _clipboard.SetBitmapAsync(((BitmapWrapper)bitmap).InnerBitmap);
        }

        public void RegisterClipboard(IClipboard clipboard)
        {
            _clipboard = clipboard;
        }
    }
}
