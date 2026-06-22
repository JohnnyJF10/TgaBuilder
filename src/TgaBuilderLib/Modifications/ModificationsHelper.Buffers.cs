namespace TgaBuilderLib.Modifications;

public partial class ModificationsHelper
{
    /// <summary>
    /// Provisions the reusable buffer set for the given input picture size. Called when the
    /// modifications view opens (with the selection presenter size) and whenever the input
    /// picture dimensions change. Cheap no-op when the size is unchanged, so callers may
    /// invoke it freely. Mirrors the transition helper's buffer system.
    /// </summary>
    public void EnsureBuffers(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive integers.");

        int pixelCount = width * height;

        if (width == Width
            && height == Height
            && _retroKeys.Length == pixelCount)
            return;

        int byteCount = pixelCount * BPP;
        int maxDimension = Math.Max(width, height);

        // Public image buffers.
        PixelsInput = new byte[byteCount];
        PixelsOutput = new byte[byteCount];
        PixelsSecondary = new byte[byteCount];

        // Color Override scratch.
        _coSecA = new float[pixelCount];
        _coSecB = new float[pixelCount];
        _coScratch = new float[pixelCount];
        _coPrefix = new float[maxDimension + 1];

        // Texture Retrofier scratch.
        _retroKeys = new int[pixelCount];
        _retroDistinct = new int[pixelCount];
        _retroCounts = new int[pixelCount];
        _retroOrder = new int[pixelCount];
        _retroDistinctToPalette = new int[pixelCount];

        Width = width;
        Height = height;

        // A resized buffer set starts without a secondary texture until one is copied in.
        ColorOverrideHasSecondary = false;
    }

    /// <summary>
    /// Releases the reusable buffer set and marks the helper inactive. Called from CleanUp
    /// when the modifications view closes.
    /// </summary>
    private void ReleaseBuffers()
    {
        Width = -1;
        Height = -1;

        PixelsInput = Array.Empty<byte>();
        PixelsOutput = Array.Empty<byte>();
        PixelsSecondary = Array.Empty<byte>();

        _coSecA = Array.Empty<float>();
        _coSecB = Array.Empty<float>();
        _coScratch = Array.Empty<float>();
        _coPrefix = Array.Empty<float>();

        _retroKeys = Array.Empty<int>();
        _retroDistinct = Array.Empty<int>();
        _retroCounts = Array.Empty<int>();
        _retroOrder = Array.Empty<int>();
        _retroDistinctToPalette = Array.Empty<int>();
        _retroBoxes.Clear();

        ColorOverrideHasSecondary = false;
    }
}
