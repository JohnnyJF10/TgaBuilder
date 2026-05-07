using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    // Builds a pixel selection (bool[Width*Height]) as the Selection pipeline step.
    // The selection is the union of all qualified tiles' pixels, optionally filtered by
    // corner-slicing trigonometry as a pre-step when SliceCornerTiles is enabled.
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

        // --- Selection Logic ---
        var cornerTileMap = SliceCornerTiles
            ? BuildCornerTileMap(labels, checkTop, checkBottom, checkLeft, checkRight)
            : null;

        for (int i = 0; i < labelCount; i++)
        {
            int labelID = i + 1;
            var segment = tileSegments[i];
            var pixelOffsets = segment.PixelOffsets;
            if (pixelOffsets.Count == 0) continue;

            float v = ComputeFocusV(Mode, (segment.CentroidX, segment.CentroidY));
            bool shouldDraw = ReversePivot ? (v <= Pivot) : (v >= Pivot);

            ReadOnlySpan<int> tileOffsets = CollectionsMarshal.AsSpan(pixelOffsets);

            // DoesTileTouchRequiredEdge needs to handle pixel indices internally
            if (shouldDraw)
                shouldDraw = !DoesTileTouchRequiredEdge(tileOffsets, !checkTop, !checkBottom, !checkLeft, !checkRight);

            if (!shouldDraw)
                shouldDraw = DoesTileTouchRequiredEdge(tileOffsets, checkTop, checkBottom, checkLeft, checkRight);

            if (!shouldDraw) continue;

            if (cornerTileMap != null && cornerTileMap.TryGetValue(labelID, out var cornerInfo))
            {
                if (cornerInfo.drawsHoriz && cornerInfo.drawsVert)
                {
                    foreach (int pixelIdx in tileOffsets)
                    {
                        selection[pixelIdx] = true;
                    }
                }
                else
                {
                    float tanAngle = ComputeCornerSliceTanAngle(cornerInfo.cx, cornerInfo.cy);
                    bool keepHorizSide = cornerInfo.drawsHoriz;

                    foreach (int pixelIdx in tileOffsets)
                    {
                        // Convert pixel index to coordinates
                        int px = pixelIdx % Width;
                        int py = pixelIdx / Width;

                        float dx = MathF.Abs(px - cornerInfo.cx);
                        float dy = MathF.Abs(py - cornerInfo.cy);

                        bool include = keepHorizSide ? (dy < dx * tanAngle) : (dy >= dx * tanAngle);
                        if (include) selection[pixelIdx] = true;
                    }
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

    // Builds a map from label ID to corner slicing info for tiles containing an image corner pixel.
    // For each tile, records whether it touches a drawn horizontal edge (top/bottom),
    // a drawn vertical edge (left/right), or both, and the first corner coordinate encountered.
    private Dictionary<int, (bool drawsHoriz, bool drawsVert, int cx, int cy)> BuildCornerTileMap(
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

        var map = new Dictionary<int, (bool drawsHoriz, bool drawsVert, int cx, int cy)>();
        for (int i = 0; i < cornerPixelIndices.Length; i++)
        {
            int pixelIdx = cornerPixelIndices[i];
            if (pixelIdx >= labels.Length) continue;

            int label = labels[pixelIdx];
            if (label <= 0) continue;

            (int cornX, int cornY) = cornerCoords[i];

            // Determine whether this image-corner position touches a drawn horizontal or vertical edge.
            bool thisHoriz = (cornY == 0 && checkTop) || (cornY == Height - 1 && checkBottom);
            bool thisVert = (cornX == 0 && checkLeft) || (cornX == Width - 1 && checkRight);

            if (map.TryGetValue(label, out var existing))
            {
                // Tile spans multiple image corners: accumulate edge-type flags.
                map[label] = (existing.drawsHoriz || thisHoriz, existing.drawsVert || thisVert,
                              existing.cx, existing.cy);
            }
            else
            {
                map[label] = (thisHoriz, thisVert, cornX, cornY);
            }
        }
        return map;
    }

    // Computes the tangent of the pivot-driven corner slice angle for the given corner position.
    //   Left / Right modes        : angle = 10° + Pivot×70°  (Pivot=0→10°, 0.5→45°, 1→80°)
    //   Top  / Bottom modes       : angle = 80° − Pivot×70°  (reversed)
    //   Diagonal modes, top corner: reversed; bottom corner: original
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ComputeCornerSliceTanAngle(int cornerX, int cornerY)
    {
        bool reverseAngle = Mode switch
        {
            TransitionMode.Top => true,
            TransitionMode.Bottom => true,
            TransitionMode.DiagonalTopLeft => cornerY == 0,
            TransitionMode.DiagonalTopRight => cornerY == 0,
            _ => false  // Left, Right: original mapping
        };

        float angleDeg = reverseAngle ? 80f - Pivot * 70f : 10f + Pivot * 70f;
        return MathF.Tan(angleDeg * MathF.PI / 180f);
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
    private float ComputeFocusV(TransitionMode mode, (float X, float Y) centroid)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Computes directional distances to texture domains for the selected transition mode.
    // Used as-is by brick transitions. Smooth transitions use ComputeTopologicyForSmooth.
    private (float distToT1, float distToT2) ComputeTopologicy(TransitionMode mode, float nx, float ny)
        => mode switch
        {
            TransitionMode.Top => (Math.Min(nx, Math.Min(1.0f - nx, 1.0f - ny)), ny),
            TransitionMode.Bottom => (Math.Min(nx, Math.Min(1.0f - nx, ny)), 1.0f - ny),
            TransitionMode.Left => (Math.Min(ny, Math.Min(1.0f - ny, 1.0f - nx)), nx),
            TransitionMode.Right => (Math.Min(ny, Math.Min(1.0f - ny, nx)), 1.0f - nx),
            TransitionMode.DiagonalTopLeft => (Math.Min(1.0f - nx, 1.0f - ny), Math.Min(nx, ny)),
            TransitionMode.DiagonalTopRight => (Math.Min(nx, 1.0f - ny), Math.Min(1.0f - nx, ny)),
            _ => (0f, 0f)
        };
}
