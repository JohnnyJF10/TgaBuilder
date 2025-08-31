

public interface ILockedBitmap : IDisposable
{
    public IntPtr BackBuffer { get; }
    public int BackBufferStride { get; }
}