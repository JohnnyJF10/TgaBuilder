using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TrLynxLib.Transitions;

public partial class TransitionHelper
{
    // Builds a pixel selection (bool[Width*Height]) as the _selection pipeline step.
    // The selection is the union of all qualified tiles' pixels, optionally filtered by
    // a per-pixel topology test for corner tiles when SliceCornerTiles is enabled.
    // The per-pixel cut uses the same ComputeFocus logic as MixSmooth at hardness=1 and
    // offset=0, so the resulting border exactly follows the ComputeTopology boundary.
    private void BuildSelection(
        List<TileSegment> tileSegments,
        int[] labels,
        byte[] tilePixels)
    {
        // Reuse the cached selection buffer. It is cleared here because the logic below only sets
        // pixels to true (it never resets them), so stale trues from a previous recalc must not
        // leak through.
        bool[] selection = _selection;
        int pixelCount = Width * Height;
        Array.Clear(selection, 0, pixelCount);
        int labelCount = tileSegments.Count;

        // Rebuild the "current occupancy" label map. It starts as the untouched segmentation (so
        // every tile stays pickable, including hidden ones) and is then patched by
        // ApplyManipulatedTiles to punch holes at vacated positions and stamp relocated footprints.
        if (_manualLabels.Length == pixelCount)
            Array.Copy(_labels, _manualLabels, pixelCount);
        _manipulatedTiles.Clear();

        TransitionDirection mode = Direction;
        bool reversePivot = ReversePivot;

        // --- Preprocessing ---
        // Determine relevant edges based on mode and reverse pivot
        (bool checkTop, bool checkBottom, bool checkLeft, bool checkRight) =
            GetDrawnEdgeTilesBools(mode, reversePivot);

        // --- _selection Logic ---
        // cornerTileSet: labels of tiles that sit at a "boundary corner" — an image corner
        // where exactly one axis (horizontal or vertical) is a drawn edge.  Only those tiles
        // need per-pixel topology testing; all others are fully included or excluded.
        var cornerTileSet = SliceCornerTiles && ProtectEdges
            ? BuildCornerTileSet(labels, checkTop, checkBottom, checkLeft, checkRight)
            : null;

        // Precompute reciprocals (only needed when corner-tile slicing is active).
        float wInv = 0f, hInv = 0f;
        if (cornerTileSet != null)
        {
            wInv = Width > 1 ? 1f / (Width - 1) : 1f;
            hInv = Height > 1 ? 1f / (Height - 1) : 1f;
        }

        for (int i = 0; i < labelCount; i++)
        {
            int labelID = i + 1;
            var segment = tileSegments[i];
            var pixelOffsets = segment.PixelOffsets;
            if (pixelOffsets.Count == 0) continue;

            float v = ComputeFocus(Direction, segment.CentroidX, segment.CentroidY, Widening, Shift);
            bool shouldDraw = segment.ShouldDrawExplicitly ?? (ReversePivot ? (v <= Pivot) : (v >= Pivot));

            // Manually moved/rotated tiles are placed explicitly by the user. They bypass the
            // edge-protection, corner-slicing and underfilling cuts and are rasterized after the
            // static selection is finalized (see ApplyManipulatedTiles), so they always land on
            // top and are never eaten into.
            bool isManipulated = segment.OffsetX != 0 || segment.OffsetY != 0 || segment.TwistAngle != 0f;
            if (isManipulated)
            {
                if (shouldDraw)
                    _manipulatedTiles.Add(ComputeManipulatedFootprint(labelID, segment));
                continue;
            }

            ReadOnlySpan<int> tileOffsets = CollectionsMarshal.AsSpan(pixelOffsets);

            // DoesTileTouchRequiredEdge needs to handle pixel indices internally
            if (ProtectEdges)
            {
                if (shouldDraw)
                    shouldDraw = !DoesTileTouchRequiredEdge(tileOffsets, !checkTop, !checkBottom, !checkLeft, !checkRight);

                if (!shouldDraw)
                    shouldDraw = DoesTileTouchRequiredEdge(tileOffsets, checkTop, checkBottom, checkLeft, checkRight);
            }

            if (!shouldDraw) continue;

            if (cornerTileSet != null && cornerTileSet.Contains(labelID))
            {
                // Per-pixel topology cut: identical to MixSmooth at hardness=1 and offset=0.
                // Each pixel is kept only if its own ComputeFocus value satisfies the same
                // draw condition, giving a cut that exactly follows the ComputeTopology boundary
                // (including the trapezoid shape at low pivot values).
                foreach (int pixelIdx in tileOffsets)
                {
                    int px = pixelIdx % Width;
                    int py = pixelIdx / Width;

                    float nx = px * wInv;
                    float ny = py * hInv;

                    float pv = ComputeFocus(Direction, nx, ny, Widening, Shift);
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

        if (UnderfillingThreshold > 0)
            SubstractUnderfilled(selection, tilePixels);

        ApplyManipulatedTiles(tileSegments, selection);
    }

    // Computes a manipulated tile's footprint by inverse mapping. For every destination pixel in
    // the transformed bounding box it finds the source pixel it samples from (R(-twist) around the
    // centroid, minus the offset) and keeps it only when that source pixel actually belongs to the
    // tile. Using inverse mapping avoids the holes a forward rotation would leave and yields one
    // unique source per destination, so no destination is ever written twice.
    private ManipulatedTile ComputeManipulatedFootprint(int label, TileSegment segment)
    {
        int width = Width;
        int height = Height;
        var pixelOffsets = segment.PixelOffsets;

        // Original axis-aligned bounding box of the tile.
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (int off in pixelOffsets)
        {
            int x = off % width;
            int y = off / width;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        // CentroidX/Y are normalized (0..1); multiplying by the dimension restores the exact
        // pixel-space centroid the tile is rotated around.
        double cx = segment.CentroidX * width;
        double cy = segment.CentroidY * height;
        int ox = segment.OffsetX;
        int oy = segment.OffsetY;

        double ang = segment.TwistAngle * Math.PI / 180.0;
        double cos = Math.Cos(ang);
        double sin = Math.Sin(ang);

        // Forward-transform the four corners to bound the destination region, then clamp to image.
        int[] cornerX = { minX, maxX, minX, maxX };
        int[] cornerY = { minY, minY, maxY, maxY };
        int dMinX = int.MaxValue, dMinY = int.MaxValue, dMaxX = int.MinValue, dMaxY = int.MinValue;
        for (int c = 0; c < 4; c++)
        {
            double rx = cos * (cornerX[c] - cx) - sin * (cornerY[c] - cy) + cx + ox;
            double ry = sin * (cornerX[c] - cx) + cos * (cornerY[c] - cy) + cy + oy;
            int lo = (int)Math.Floor(rx);
            int hi = (int)Math.Ceiling(rx);
            if (lo < dMinX) dMinX = lo;
            if (hi > dMaxX) dMaxX = hi;
            lo = (int)Math.Floor(ry);
            hi = (int)Math.Ceiling(ry);
            if (lo < dMinY) dMinY = lo;
            if (hi > dMaxY) dMaxY = hi;
        }

        // Expand by 1px so nearest-neighbor rounding at the rotated edges can never clip the tile,
        // then clamp to the image (out-of-image parts are dropped by the per-pixel bounds check).
        dMinX -= 1; dMinY -= 1; dMaxX += 1; dMaxY += 1;
        if (dMinX < 0) dMinX = 0;
        if (dMinY < 0) dMinY = 0;
        if (dMaxX > width - 1) dMaxX = width - 1;
        if (dMaxY > height - 1) dMaxY = height - 1;

        var dst = new List<int>();
        var src = new List<int>();

        for (int dy = dMinY; dy <= dMaxY; dy++)
        {
            for (int dx = dMinX; dx <= dMaxX; dx++)
            {
                double px = dx - ox - cx;
                double py = dy - oy - cy;

                // Inverse rotation R(-twist).
                double sxf = cos * px + sin * py + cx;
                double syf = -sin * px + cos * py + cy;

                // Standard nearest-neighbor rounding (Math.Round would use banker's rounding and
                // could drop pixels asymmetrically at .5 boundaries during rotation).
                int sxi = (int)Math.Floor(sxf + 0.5);
                int syi = (int)Math.Floor(syf + 0.5);

                if (sxi < 0 || sxi >= width || syi < 0 || syi >= height)
                    continue;

                int srcIdx = syi * width + sxi;
                if (_labels[srcIdx] != label)
                    continue;

                dst.Add(dy * width + dx);
                src.Add(srcIdx);
            }
        }

        return new ManipulatedTile
        {
            Label = label,
            DstOffsets = dst.ToArray(),
            SrcOffsets = src.ToArray()
        };
    }

    // Finalizes the manual moves/rotations collected during BuildSelection:
    //   1) reorders so the active tile is processed last (so it wins overlaps / draws on top),
    //   2) adds each transformed footprint to the selection so shadows, edges and the final mask
    //      follow the new position (underfilling has already run, so it can't eat a placed tile),
    //   3) punches the manipulated tiles' original pixels out of the manual label map (the holes
    //      they left), then stamps the transformed footprints in. Punching for all tiles before
    //      stamping prevents a footprint that lands on another tile's vacated pixels from being
    //      cleared again.
    private void ApplyManipulatedTiles(List<TileSegment> tileSegments, bool[] selection)
    {
        if (_manipulatedTiles.Count == 0)
            return;

        int activeLabel = ActiveManipulatedTileLabel;
        if (activeLabel > 0)
        {
            int activeIdx = _manipulatedTiles.FindIndex(t => t.Label == activeLabel);
            if (activeIdx >= 0 && activeIdx != _manipulatedTiles.Count - 1)
            {
                var active = _manipulatedTiles[activeIdx];
                _manipulatedTiles.RemoveAt(activeIdx);
                _manipulatedTiles.Add(active);
            }
        }

        foreach (var tile in _manipulatedTiles)
        {
            int[] dst = tile.DstOffsets;
            for (int i = 0; i < dst.Length; i++)
                selection[dst[i]] = true;
        }

        if (_manualLabels.Length != Width * Height)
            return;

        foreach (var tile in _manipulatedTiles)
        {
            var original = tileSegments[tile.Label - 1].PixelOffsets;
            foreach (int off in original)
                _manualLabels[off] = 0;
        }

        foreach (var tile in _manipulatedTiles)
        {
            int[] dst = tile.DstOffsets;
            for (int i = 0; i < dst.Length; i++)
                _manualLabels[dst[i]] = tile.Label;
        }
    }

    private void SubstractUnderfilled(bool[] selection, byte[] tilePixels)
    {
        int stride = Width * TRANSITIONS_BPP;

        unsafe
        {
            fixed (byte* pTile = tilePixels)
            {
                for (int y = 0; y < Height; y++)
                {
                    byte* rowValley = pTile + y * stride;

                    float ny = (float)y / (Height - 1);

                    for (int x = 0; x < Width; x++)
                    {
                        float nx = (float)x / (Width - 1);

                        float v = ComputeFocus(Direction, nx, ny, Widening, Shift);

                        byte* pxValley = rowValley + x * TRANSITIONS_BPP;

                        int grey = ReverseUnderfilling
                            ? (int)(255f - (pxValley[2] * 0.299f + pxValley[1] * 0.587f + pxValley[0] * 0.114f))
                            : (int)(pxValley[2] * 0.299f + pxValley[1] * 0.587f + pxValley[0] * 0.114f);

                        bool Underfilling = grey < UnderfillingThreshold && (ReversePivot ? (v >= UnderfillingPivot) : (v < UnderfillingPivot));

                        if (Underfilling)
                        {
                            for (int b = 0; b < TRANSITIONS_BPP; b++)
                            {
                                selection[y * Width + x] = false;
                            }
                        }
                    }
                }
            }
        }
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
            bool thisVert = (cornX == 0 && checkLeft) || (cornX == Width - 1 && checkRight);

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
        TransitionDirection mode,
        bool reversePivot)
        => (mode, reversePivot) switch
        {
            (TransitionDirection.Top, false) => (true, false, false, false),
            (TransitionDirection.Top, true) => (false, true, true, true),
            (TransitionDirection.Bottom, false) => (false, true, false, false),
            (TransitionDirection.Bottom, true) => (true, false, true, true),
            (TransitionDirection.Left, false) => (false, false, true, false),
            (TransitionDirection.Left, true) => (true, true, false, true),
            (TransitionDirection.Right, false) => (false, false, false, true),
            (TransitionDirection.Right, true) => (true, true, true, false),
            (TransitionDirection.DiagonalTopLeft, false) => (true, false, true, false),
            (TransitionDirection.DiagonalTopLeft, true) => (false, true, false, true),
            (TransitionDirection.DiagonalTopRight, false) => (true, false, false, true),
            (TransitionDirection.DiagonalTopRight, true) => (false, true, true, false),
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
}
