namespace TgaBuilderLib.Transitions;

/// <summary>
/// One layer produced for a multi-layer transition export. Pixels are straight-alpha
/// BGRA8888, row-major, stride = <see cref="Width"/> * 4.
/// </summary>
public sealed class TransitionExportLayer
{
    public TransitionExportLayer(byte[] bgra, int width, int height, string name, bool visible)
    {
        Bgra = bgra ?? throw new ArgumentNullException(nameof(bgra));
        Width = width;
        Height = height;
        Name = name;
        Visible = visible;
    }

    /// <summary>Straight-alpha BGRA8888 pixels, row-major, stride = Width * 4.</summary>
    public byte[] Bgra { get; }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Layer display name.</summary>
    public string Name { get; }

    /// <summary>Whether the layer is visible when the file is opened.</summary>
    public bool Visible { get; }
}
