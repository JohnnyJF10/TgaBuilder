using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
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

        public bool ContainsImage()
        {
            var bitmap = _clipboard?.TryGetBitmapAsync().Result;
            return bitmap != null;
        }

        public async Task<IReadableBitmap?> GetImageAsync()
        {
            if (_clipboard is null) 
                throw new InvalidOperationException("Clipboard service is not initialized.");

            var bitmap = await _clipboard.TryGetBitmapAsync();
            if (bitmap is not null)
                return new BitmapWrapper(bitmap);
            else throw new InvalidOperationException("Clipboard does not contain an image.");
        }

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
