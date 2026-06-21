using Avalonia.Platform;
using System;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Wrappers
{
    internal class BitmapLocker : IBitmapLocker
    {
        private readonly ILockedFramebuffer _lockedFramebuffer;

        public BitmapLocker(ILockedFramebuffer lockedFramebuffer)
        {
            _lockedFramebuffer = lockedFramebuffer;
        }

        public IntPtr BackBuffer => _lockedFramebuffer.Address;

        public void Dispose()
        {
            _lockedFramebuffer.Dispose();
        }
    }
}
