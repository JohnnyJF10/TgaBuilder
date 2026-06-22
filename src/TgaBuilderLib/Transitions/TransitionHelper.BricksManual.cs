using System.Runtime.CompilerServices;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    public bool SetExplicitTileVisibility(int tileIndex, bool shouldDraw)
    {
        if (tileIndex < 0 || tileIndex >= _tileSegmentList.Count + 1)
            throw new ArgumentOutOfRangeException(nameof(tileIndex), "Tile index is out of range.");

        int listIndex = tileIndex - 1;
        var segment = _tileSegmentList[listIndex];

        bool? currentValue = segment.ShouldDrawExplicitly;
        segment.ShouldDrawExplicitly = shouldDraw;

        bool changed = !currentValue.HasValue || currentValue.Value != shouldDraw;

        // Erasing a tile returns it to its original position and orientation: a hidden tile should
        // not retain a manual move/rotation, so revealing it again starts clean.
        if (!shouldDraw && (segment.OffsetX != 0 || segment.OffsetY != 0 || segment.TwistAngle != 0f))
        {
            segment.OffsetX = 0;
            segment.OffsetY = 0;
            segment.TwistAngle = 0f;
            changed = true;
        }

        return changed;
    }

    // Picks the tile that can be manually moved/rotated at the given result-image pixel. Uses the
    // current occupancy map, so a moved tile is picked at its new position and a vacated hole reads
    // as background. Returns 0 (none) for background/holes and — when ProtectEdges is set — for
    // tiles touching an image border, which must stay put to keep the transition seamless.
    public int PickManipulableTileAt(int x, int y)
    {
        int label = GetLabelAtPixel(x, y);
        if (label == 0)
            return 0;

        int listIndex = label - 1;
        if (listIndex < 0 || listIndex >= _tileSegmentList.Count)
            return 0;

        if (ProtectEdges && DoesTileTouchAnyEdge(_tileSegmentList[listIndex].PixelOffsets))
            return 0;

        return label;
    }

    // Current manual translation (in result-image pixels) of the given tile. Used by the view model
    // to anchor a drag to the tile's existing offset.
    public (int X, int Y) GetTileOffset(int tileLabel)
    {
        int listIndex = tileLabel - 1;
        if (listIndex < 0 || listIndex >= _tileSegmentList.Count)
            return (0, 0);

        var segment = _tileSegmentList[listIndex];
        return (segment.OffsetX, segment.OffsetY);
    }

    // Sets a tile's absolute manual translation (in result-image pixels) via its Offset properties.
    // A moved tile is always made visible (ShouldDrawExplicitly = true), even if it was hidden
    // before. Returns true if anything changed.
    public bool MoveTile(int tileLabel, int offsetX, int offsetY)
    {
        int listIndex = tileLabel - 1;
        if (listIndex < 0 || listIndex >= _tileSegmentList.Count)
            return false;

        var segment = _tileSegmentList[listIndex];

        bool changed = segment.OffsetX != offsetX || segment.OffsetY != offsetY;
        segment.OffsetX = offsetX;
        segment.OffsetY = offsetY;

        if (segment.ShouldDrawExplicitly != true)
        {
            segment.ShouldDrawExplicitly = true;
            changed = true;
        }

        return changed;
    }

    // Current manual rotation (in degrees) of the given tile. Used by the view model to anchor a
    // drag-to-rotate gesture to the tile's existing angle.
    public float GetTileTwist(int tileLabel)
    {
        int listIndex = tileLabel - 1;
        if (listIndex < 0 || listIndex >= _tileSegmentList.Count)
            return 0f;

        return _tileSegmentList[listIndex].TwistAngle;
    }

    // Sets a tile's absolute manual rotation (in degrees) about its centroid via its TwistAngle
    // property. A rotated tile is always made visible. Returns true if anything changed. Used by the
    // drag-to-rotate gesture; the mouse wheel uses the incremental RotateTileBy.
    public bool SetTileTwist(int tileLabel, float angleDegrees)
    {
        int listIndex = tileLabel - 1;
        if (listIndex < 0 || listIndex >= _tileSegmentList.Count)
            return false;

        var segment = _tileSegmentList[listIndex];

        bool changed = segment.TwistAngle != angleDegrees;
        segment.TwistAngle = angleDegrees;

        if (segment.ShouldDrawExplicitly != true)
        {
            segment.ShouldDrawExplicitly = true;
            changed = true;
        }

        return changed;
    }

    // Adds a delta (in degrees) to a tile's manual rotation about its centroid via its TwistAngle
    // property. A rotated tile is always made visible. Returns true if anything changed.
    public bool RotateTileBy(int tileLabel, float deltaDegrees)
    {
        int listIndex = tileLabel - 1;
        if (listIndex < 0 || listIndex >= _tileSegmentList.Count)
            return false;

        var segment = _tileSegmentList[listIndex];

        bool changed = false;
        if (deltaDegrees != 0f)
        {
            segment.TwistAngle += deltaDegrees;
            changed = true;
        }

        if (segment.ShouldDrawExplicitly != true)
        {
            segment.ShouldDrawExplicitly = true;
            changed = true;
        }

        return changed;
    }

    // Returns everything to the state before any manual interaction: visibility overrides cleared
    // and all moves/rotations undone.
    public void ResetAllExplicitTileVisibility()
    {
        foreach (var segment in _tileSegmentList)
        {
            segment.ShouldDrawExplicitly = null;
            segment.OffsetX = 0;
            segment.OffsetY = 0;
            segment.TwistAngle = 0f;
        }

        _manipulatedTiles.Clear();
        ActiveManipulatedTileLabel = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool DoesTileTouchAnyEdge(List<int> pixelOffsets)
    {
        int width = Width;
        int lastX = width - 1;
        int lastY = Height - 1;

        foreach (int offset in pixelOffsets)
        {
            int y = offset / width;
            int x = offset % width;

            if (x == 0 || y == 0 || x == lastX || y == lastY)
                return true;
        }

        return false;
    }
}
