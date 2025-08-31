
public interface IReadableBitmap
{
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public bool HasAlpha { get; }
    public int Size { get; }
}
