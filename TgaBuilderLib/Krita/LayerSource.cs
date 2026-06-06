namespace TgaBuilderLib.Krita;

/// <summary>One source image that becomes one paint layer.</summary>
public sealed class LayerSource
{
    public required string Name;        // human readable layer name (the source file name)
    public required string FileName;    // on-disk name inside the .kra (e.g. "layer1")
    public required byte[] Bgra;        // straight-alpha BGRA8888, row-major, stride = Width*4
    public required int Width;
    public required int Height;
    public string Uuid = "{" + Guid.NewGuid().ToString() + "}";
}