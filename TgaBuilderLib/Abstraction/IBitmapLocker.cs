

public interface IBitmapLocker : IDisposable
{
    public IntPtr BackBuffer { get; }
    public int Stride { get; }
    public int Width { get; }
    public int Height { get; }
}