using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TgaBuilderLib.Transitions
{
    partial class TransitionHelper
    {
        // Draws segmented tile pixels over a background using a pixel selection derived from
        // tile topology and optional corner slicing. Pipeline: Input → Label Map → Selection → Result.
        public byte[] MixSmartTilesPixels(
          byte[] tilePixels,
          byte[] bgPixels)
        {
            if (bgPixels.Length != tilePixels.Length)
                throw new ArgumentException("Pixel arrays must have same length.");

            var currentTileData = new List<TileSegment>(TileData.Count);
            foreach (var tile in TileData)         
                currentTileData.Add((TileSegment)tile.Clone());

            var currentLabels = new int[Labels.Length];
            Array.Copy(Labels, currentLabels, Labels.Length);

            if (currentLabels.Length > 0 && currentLabels.Max() > currentTileData.Count)
                return bgPixels; // Fallback in case of race condition

            // Determine relevant edges based on mode and reverse pivot
            (bool checkTop, bool checkBottom, bool checkLeft, bool checkRight) =
                GetDrawnEdgeTilesBools(Mode, ReversePivot);

            // Selection step: determine which pixels are drawn (Input → Label Map → Selection)
            bool[] selection = BuildSelection(currentTileData, currentLabels,
                checkTop, checkBottom, checkLeft, checkRight);

            byte[] result = new byte[bgPixels.Length];

            unsafe
            {
                fixed (byte* pBg = bgPixels)
                fixed (byte* pTile = tilePixels)
                fixed (byte* pRes = result)
                {
                    // Copy the background first
                    Buffer.MemoryCopy(pBg, pRes, Height * Stride, Height * Stride);

                    // Apply selection: copy selected pixels from tile texture
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            if (!selection[y * Width + x]) continue;

                            int offset = y * Stride + x * TRANSITIONS_BPP;
                            for (int b = 0; b < TRANSITIONS_BPP; b++)
                                pRes[offset + b] = pTile[offset + b];
                        }
                    }

                    // Contour smoothing at the demolition edge selection border only.
                    // Pixels positioned on the texture outer edge are excluded from smoothing.
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            if (!selection[y * Width + x]) continue;

                            // No smoothing for pixels on the texture outer edge
                            if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1) continue;

                            int backgroundNeighbors = 0;
                            if (!selection[(y - 1) * Width + x]) backgroundNeighbors++;
                            if (!selection[(y + 1) * Width + x]) backgroundNeighbors++;
                            if (!selection[y * Width + (x - 1)]) backgroundNeighbors++;
                            if (!selection[y * Width + (x + 1)]) backgroundNeighbors++;

                            if (backgroundNeighbors > 0)
                            {
                                int offset = y * Stride + x * TRANSITIONS_BPP;
                                for (int b = 0; b < TRANSITIONS_BPP; b++)
                                    pRes[offset + b] = (byte)((pTile[offset + b] + pBg[offset + b]) >> 1);
                            }
                        }
                    }
                }
            }
            return result;
        }

        // Builds a pixel selection (bool[Width*Height]) as the Selection pipeline step.
        // The selection is the union of all qualified tiles' pixels, optionally filtered by
        // corner-slicing trigonometry as a pre-step when SliceCornerTiles is enabled.
        private bool[] BuildSelection(
            List<TileSegment> tileData,
            int[] labels,
            bool checkTop, bool checkBottom, bool checkLeft, bool checkRight)
        {
            bool[] selection = new bool[Width * Height];

            // Optional pre-step: identify corner tiles for pixel-level trigonometric filtering
            var cornerTileMap = SliceCornerTiles ? BuildCornerTileMap(labels) : null;

            for (int i = 0; i < tileData.Count; i++)
            {
                var tile = tileData[i];
                int labelID = i + 1;

                // Pivot condition
                float v = ComputeFocusV(Mode, tile);
                bool shouldDraw = ReversePivot ? (v <= Pivot) : (v >= Pivot);

                // Avoid background-touching edge tiles being drawn
                if (shouldDraw)
                    shouldDraw = !DoesTileTouchRequiredEdge(tile, !checkTop, !checkBottom, !checkLeft, !checkRight);

                // Ensure required edge tiles are always drawn
                if (!shouldDraw)
                    shouldDraw = DoesTileTouchRequiredEdge(tile, checkTop, checkBottom, checkLeft, checkRight);

                if (!shouldDraw)
                    continue;

                // Corner slicing pre-step: for corner tiles, only add pixels that satisfy
                // the trigonometric condition (same side as the corner pixel itself)
                if (cornerTileMap != null && cornerTileMap.TryGetValue(labelID, out var corner))
                {
                    float tanAngle = ComputeCornerSliceTanAngle(corner.cx, corner.cy);

                    foreach (int offset in tile.PixelOffsets)
                    {
                        int py = offset / Stride;
                        int px = (offset % Stride) / TRANSITIONS_BPP;

                        float dx = MathF.Abs(px - corner.cx);
                        float dy = MathF.Abs(py - corner.cy);

                        // The corner pixel itself satisfies dy >= dx * tanAngle (0 >= 0).
                        // Include only pixels on the same side as the corner.
                        if (dy >= dx * tanAngle)
                            selection[py * Width + px] = true;
                    }
                }
                else
                {
                    foreach (int offset in tile.PixelOffsets)
                    {
                        int py = offset / Stride;
                        int px = (offset % Stride) / TRANSITIONS_BPP;
                        selection[py * Width + px] = true;
                    }
                }
            }

            return selection;
        }

        // Builds a map from label ID to corner coordinates for tiles that contain an image corner pixel.
        private Dictionary<int, (int cx, int cy)> BuildCornerTileMap(int[] labels)
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

            var map = new Dictionary<int, (int cx, int cy)>();
            for (int i = 0; i < cornerPixelIndices.Length; i++)
            {
                int pixelIdx = cornerPixelIndices[i];
                if (pixelIdx < labels.Length)
                {
                    int label = labels[pixelIdx];
                    if (label > 0 && !map.ContainsKey(label))
                        map[label] = cornerCoords[i];
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
        private bool DoesTileTouchRequiredEdge(TileSegment tile, bool top, bool bottom, bool left, bool right)
        {
            int touchPixCount = 0;

            foreach (int offset in tile.PixelOffsets)
            {
                int y = offset / Stride;
                int x = (offset % Stride) / TRANSITIONS_BPP;

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
        private float ComputeFocusV(TransitionMode mode, TileSegment tile)
        {
            float nx = tile.CentroidX;
            float ny = tile.CentroidY;

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
}
