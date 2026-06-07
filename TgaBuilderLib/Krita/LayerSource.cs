namespace TgaBuilderLib.Krita;

/// <summary>One source image that becomes one paint layer.</summary>
public sealed class LayerSource
{
    public LayerSource(byte[] bgra, int width, int height, string name = "")
    {
        Bgra = bgra ?? throw new ArgumentNullException(nameof(bgra));
        Width = width;
        Height = height;
        Name = name;
    }


    public string Name;       // human readable layer name (the source file name)
    public byte[] Bgra;       // straight-alpha BGRA8888, row-major, stride = Width*4
    public int Width;
    public int Height;
    public string Uuid = "{" + Guid.NewGuid().ToString() + "}";
}