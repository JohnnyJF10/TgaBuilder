using TrLynxLib.Abstraction;

namespace TrLynxLib.Psd;

/// <summary>
/// Holds the data for a single layer when saving a PSD file.
/// </summary>
public class PsdLayerInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PsdLayerInfo"/> class.
    /// </summary>
    /// <param name="bitmap">
    /// The layer's pixel data in BGRA 32-bit format.
    /// Its dimensions must match <paramref name="rect"/>.
    /// </param>
    /// <param name="rect">
    /// Position and size of the layer within the document canvas.
    /// </param>
    /// <param name="name">
    /// Display name of the layer.
    /// </param>
    /// <param name="opacity">
    /// Opacity of the layer, where 0 is fully transparent and 255 is fully opaque.
    /// </param>
    /// <param name="visible">
    /// Whether the layer is visible.
    /// </param>
    /// <param name="blendModeKey">
    /// Four-character PSD blend mode key (for example, "norm" for Normal or
    /// "mul " for Multiply). The default value is "norm".
    /// </param>
    public PsdLayerInfo(
        IReadableBitmap bitmap,
        PixelRect rect,
        string name,
        byte opacity = 255,
        bool visible = true,
        string blendModeKey = "norm")
    {
        if (blendModeKey.Length != 4)
            throw new ArgumentException("Blend mode key must be exactly 4 characters.", nameof(blendModeKey));

        Bitmap = bitmap;
        Rect = rect;
        Name = name;
        Opacity = opacity;
        Visible = visible;
        BlendModeKey = blendModeKey;
    }

    /// <summary>The layer's pixel data in BGRA 32-bit format. Dimensions must match <see cref="Rect"/>.</summary>
    public IReadableBitmap Bitmap { get; }

    /// <summary>Position and size of the layer within the document canvas.</summary>
    public PixelRect Rect { get; }

    /// <summary>The layer's display name.</summary>
    public string Name { get; }

    /// <summary>0 = transparent, 255 = opaque.</summary>
    public byte Opacity { get; }

    /// <summary>Whether the layer is visible.</summary>
    public bool Visible { get; }

    /// <summary>
    /// Four-character PSD blend mode key
    /// (e.g. "norm" = Normal, "mul " = Multiply, "scrn" = Screen).
    /// </summary>
    public string BlendModeKey { get; }
}
