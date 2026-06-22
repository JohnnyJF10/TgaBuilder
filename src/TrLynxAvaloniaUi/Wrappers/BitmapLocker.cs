using Avalonia.Platform;
using System;
using TrLynxLib.Abstraction;

namespace TrLynxAvaloniaUi.Wrappers
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
