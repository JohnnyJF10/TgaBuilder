namespace TgaBuilderLib.Krita;

/// <summary>One source image that becomes one paint layer.</summary>
public sealed class LayerSource
{
    public LayerSource(byte[] bgra, int width, int height, bool hasAlpha, string name = "", bool visible = true)
    {
        PixelBytes = bgra ?? throw new ArgumentNullException(nameof(bgra));
        Width = width;
        Height = height;
        HasAlpha = hasAlpha;
        Name = name;
        Visible = visible;
    }

    public readonly string Name;                     // human readable layer name (the source file name)
    public readonly byte[] PixelBytes;               // straight-alpha BGRA8888, row-major, stride = Width*4
    public readonly int Width;
    public readonly int Height;
    public readonly bool HasAlpha;
    public readonly bool Visible;                    // whether the layer is shown when the file is opened
    public readonly string Uuid = "{" + Guid.NewGuid().ToString() + "}";
}