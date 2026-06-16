namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    /// <summary>
    /// Provisions the reusable buffer set for the given input picture size. Called when the
    /// transitions view opens and whenever the input picture dimensions change. Cheap no-op
    /// when the size is unchanged, so callers may invoke it freely.
    /// </summary>
    public void EnsureBuffers(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive integers.");

        int n = width * height;

        if (width == Width
            && height == Height
            && _scratchFiltered.Length == n)
            return;

        int n4 = n * TRANSITIONS_BPP;

        Pixels1 = new byte[n4];
        Pixels2 = new byte[n4];
        PixelsResult = new byte[n4];

        _labels = new int[n];
        _selection = new bool[n];
        _scratchFiltered = new float[n];
        _scratchGray = new float[n];
        _scratchFilteredColor = new byte[n4];
        _scratchShadowedBg = new byte[n4];
        _scratchLabelMap = new byte[n4];
        _edgeDist = new int[n];
        _manualLabels = new int[n];

        // The tile set changes with the picture size, so any pending manual move/rotate state
        // no longer maps to a valid tile and must be dropped.
        _manipulatedTiles.Clear();
        ActiveManipulatedTileLabel = 0;

        Width = width;
        Height = height;

        _labelsBuilt = false;
        _selectionBuilt = false;
        _edgeDistValid = false;
    }
}