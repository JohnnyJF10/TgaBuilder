

namespace TgaBuilderLib.Abstraction
{
    public interface IBitmapLocker : IDisposable
    {
        /// <summary>
        /// Pointer to the back buffer of the bitmap.
        /// </summary>
        public IntPtr BackBuffer { get; }
    }
}
