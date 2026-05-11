using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    // Builds a pixel selection (bool[Width*Height]) as the _selection pipeline step.
    // The selection is the union of all qualified tiles' pixels, optionally filtered by
    // a per-pixel topology test for corner tiles when SliceCornerTiles is enabled.
    // The per-pixel cut uses the same ComputeFocusValue logic as MixSmooth at hardness=1 and
    // offset=0, so the resulting border exactly follows the ComputeTopology boundary.
    private bool[] BuildSelection(
        List<TileSegment> tileSegments,
        int[] labels,
        TransitionMode mode,
        bool reversePivot)
    {
        bool[] selection = new bool[Width * Height];
        int labelCount = tileSegments.Count;

        // --- Preprocessing ---
        // Determine relevant edges based on mode and reverse pivot
        (bool checkTop, bool checkBottom, bool checkLeft, bool checkRight) =
            GetDrawnEdgeTilesBools(mode, reversePivot);

        // --- _selection Logic ---
        // cornerTileSet: labels of tiles that sit at a "boundary corner" — an image corner
        // where exactly one axis (horizontal or vertical) is a drawn edge.  Only those tiles
        // need per-pixel topology testing; all others are fully included or excluded.
        var cornerTileSet = SliceCornerTiles
            ? BuildCornerTileSet(labels, checkTop, checkBottom, checkLeft, checkRight)
            : null;

        // Precompute reciprocals (only needed when corner-tile slicing is active).
        float wInv = 0f, hInv = 0f;
        if (cornerTileSet != null)
        {
            wInv = Width  > 1 ? 1f / (Width  - 1) : 1f;
            hInv = Height > 1 ? 1f / (Height - 1) : 1f;
        }

        for (int i = 0; i < labelCount; i++)
        {
            int labelID = i + 1;
            var segment = tileSegments[i];
            var pixelOffsets = segment.PixelOffsets;
            if (pixelOffsets.Count == 0) continue;

            float v = ComputeFocusValue(Mode, (segment.CentroidX, segment.CentroidY));
            bool shouldDraw = ReversePivot ? (v <= Pivot) : (v >= Pivot);

            ReadOnlySpan<int> tileOffsets = CollectionsMarshal.AsSpan(pixelOffsets);

            // DoesTileTouchRequiredEdge needs to handle pixel indices internally
            if (shouldDraw)
                shouldDraw = !DoesTileTouchRequiredEdge(tileOffsets, !checkTop, !checkBottom, !checkLeft, !checkRight);

            if (!shouldDraw)
                shouldDraw = DoesTileTouchRequiredEdge(tileOffsets, checkTop, checkBottom, checkLeft, checkRight);

            if (!shouldDraw) continue;

            if (cornerTileSet != null && cornerTileSet.Contains(labelID))
            {
                // Per-pixel topology cut: identical to MixSmooth at hardness=1 and offset=0.
                // Each pixel is kept only if its own ComputeFocusValue value satisfies the same
                // draw condition, giving a cut that exactly follows the ComputeTopology boundary
                // (including the trapezoid shape at low pivot values).
                foreach (int pixelIdx in tileOffsets)
                {
                    int px = pixelIdx % Width;
                    int py = pixelIdx / Width;

                    float nx = px * wInv;
                    float ny = py * hInv;

                    float pv = ComputeFocusValue(Mode, (nx, ny));
                    bool include = ReversePivot ? (pv <= Pivot) : (pv >= Pivot);
                    if (include) selection[pixelIdx] = true;
                }
            }
            else
            {
                foreach (int pixelIdx in tileOffsets)
                {
                    selection[pixelIdx] = true;
                }
            }
        }

        return selection;
    }

    // Returns the set of label IDs for tiles that require a per-pixel topology cut.
    // A tile qualifies when it touches at least one image corner that lies at the boundary
    // between a drawn edge and a non-drawn edge (exactly one of the two meeting edges is
    // a drawn edge — the XOR condition).  Corners that collapse to the same pixel index
    // (degenerate Width==1 or Height==1 images) are deduplicated.
    private HashSet<int> BuildCornerTileSet(
        int[] labels,
        bool checkTop, bool checkBottom, bool checkLeft, bool checkRight)
    {
        int[] cornerPixelIndices = new int[]
        {
                0,                                    // top-left  (0, 0)
                Width - 1,                            // top-right (W-1, 0)
                (Height - 1) * Width,                 // bottom-left  (0, H-1)
                (Height - 1) * Width + (Width - 1)    // bottom-right (W-1, H-1)
        };

        (int cx, int cy)[] cornerCoords = new (int, int)[]
        {
                (0, 0),
                (Width - 1, 0),
                (0, Height - 1),
                (Width - 1, Height - 1)
        };

        var set = new HashSet<int>();
        var processedPixels = new HashSet<int>();

        for (int i = 0; i < cornerPixelIndices.Length; i++)
        {
            int pixelIdx = cornerPixelIndices[i];
            if (pixelIdx >= labels.Length) continue;
            if (!processedPixels.Add(pixelIdx)) continue; // skip duplicate corner positions

            int label = labels[pixelIdx];
            if (label <= 0) continue;

            (int cornX, int cornY) = cornerCoords[i];

            bool thisHoriz = (cornY == 0 && checkTop) || (cornY == Height - 1 && checkBottom);
            bool thisVert  = (cornX == 0 && checkLeft) || (cornX == Width  - 1 && checkRight);

            // Only corners where exactly one axis is a drawn edge (XOR) define the transition
            // boundary and require per-pixel cutting.
            if (thisHoriz != thisVert)
                set.Add(label);
        }
        return set;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Returns which outer edges are required for drawing in the current transition direction.
    private (bool checkTop, bool checkBottom, bool checkLeft, bool checkRight) GetDrawnEdgeTilesBools(
        TransitionMode mode,
        bool reversePivot)
        => (mode, reversePivot) switch
        {
            (TransitionMode.Top, false) => (true, false, false, false),
            (TransitionMode.Top, true) => (false, true, true, true),
            (TransitionMode.Bottom, false) => (false, true, false, false),
            (TransitionMode.Bottom, true) => (true, false, true, true),
            (TransitionMode.Left, false) => (false, false, true, false),
            (TransitionMode.Left, true) => (true, true, false, true),
            (TransitionMode.Right, false) => (false, false, false, true),
            (TransitionMode.Right, true) => (true, true, true, false),
            (TransitionMode.DiagonalTopLeft, false) => (true, false, true, false),
            (TransitionMode.DiagonalTopLeft, true) => (false, true, false, true),
            (TransitionMode.DiagonalTopRight, false) => (true, false, false, true),
            (TransitionMode.DiagonalTopRight, true) => (false, true, true, false),
            _ => throw new ArgumentException("Invalid mode.")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Checks whether a tile touches any of the requested image edges.
    private bool DoesTileTouchRequiredEdge(ReadOnlySpan<int> pixelOffsets, bool top, bool bottom, bool left, bool right)
    {
        int touchPixCount = 0;

        foreach (int offset in pixelOffsets)
        {
            int y = offset / Width;
            int x = offset % Width;

            if (top && y == 0)
                touchPixCount++;

            if (bottom && y == Height - 1)
                touchPixCount++;

            if (left && x == 0)
                touchPixCount++;

            if (right && x == Width - 1)
                touchPixCount++;

            if (touchPixCount > 2)
                return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Computes a normalized focus value for a tile based on its centroid.
    private float ComputeFocusValue(TransitionMode mode, (float X, float Y) centroid)
    {
        float nx = centroid.X;
        float ny = centroid.Y;

        // --- Topological logic excerpt ---
        float distToT1 = 0, distToT2 = 0;

        (distToT1, distToT2) = ComputeTopologicy(mode, nx, ny);

        float v;
        if (distToT2 <= 0.00001f) v = 1.0f;
        else if (distToT1 <= 0.00001f) v = 0.0f;
        else v = distToT1 / (distToT1 + distToT2);
        return v;
    }
}
