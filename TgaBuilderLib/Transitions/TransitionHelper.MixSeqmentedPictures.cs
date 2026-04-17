using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TgaBuilderLib.Transitions
{
    partial class TransitionHelper
    {
        // Draws segmented tile pixels over a background according to transition progress and edge constraints.
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

            if (currentLabels.Max() > currentTileData.Count)
                return bgPixels; // Fallback in case of race condition

            bool[] isLabelDrawn = new bool[currentTileData.Count + 1];
            List<int> drawnPixelsOffsets = new List<int>(Width * Height);
            byte[] result = new byte[bgPixels.Length];

            // Determine relevant edges based on mode and reverse pivot
            bool checkTop = false, checkBottom = false, checkLeft = false, checkRight = false;

            (checkTop, checkBottom, checkLeft, checkRight) = GetDrawnEdgeTilesBools(Mode, ReversePivot);

            unsafe
            {
                fixed (byte* pBg = bgPixels)
                fixed (byte* pTile = tilePixels)
                fixed (byte* pRes = result)
                {
                    // Copy the background first
                    Buffer.MemoryCopy(pBg, pRes, Height * Stride, Height * Stride);

                    for (int i = 0; i < currentTileData.Count; i++)
                    {
                        var tile = currentTileData[i];
                        int labelID = i + 1;

                        // 1. Pivot condition (v is the computed progress value of this tile)
                        float v = ComputeFocusV(Mode, tile);
                        bool shouldDraw = ReversePivot ? (v <= Pivot) : (v >= Pivot);

                        // 2. Edge condition (check edge tiles)

                        // Avoid background touching edges tiles being drawn
                        if (shouldDraw)
                        {
                            shouldDraw = !DoesTileTouchRequiredEdge(tile, !checkTop, !checkBottom, !checkLeft, !checkRight);
                        }

                        // Make sure tile touching edges are drawn
                        if (!shouldDraw)
                        {
                            shouldDraw = DoesTileTouchRequiredEdge(tile, checkTop, checkBottom, checkLeft, checkRight);
                        }

                        if (shouldDraw)
                        {
                            isLabelDrawn[labelID] = true;
                            foreach (int offset in tile.PixelOffsets)
                            {
                                for (int b = 0; b < Bpp; b++) { pRes[offset + b] = pTile[offset + b]; }
                                drawnPixelsOffsets.Add(offset);
                            }
                        }
                    }

                    // 3. Contour smoothing (antialiasing pass)
                    foreach (int offset in drawnPixelsOffsets)
                    {
                        int y = offset / Stride;
                        int x = (offset % Stride) / Bpp;
                        int backgroundNeighbors = 0;

                        // Check neighbors in the labels array
                        if (y > 0 && !isLabelDrawn[currentLabels[(y - 1) * Width + x]])
                            backgroundNeighbors++;

                        if (y < Height - 1 && !isLabelDrawn[currentLabels[(y + 1) * Width + x]])
                            backgroundNeighbors++;

                        if (x > 0 && !isLabelDrawn[currentLabels[y * Width + (x - 1)]])
                            backgroundNeighbors++;

                        if (x < Width - 1 && !isLabelDrawn[currentLabels[y * Width + (x + 1)]])
                            backgroundNeighbors++;

                        if (backgroundNeighbors > 0)
                        {
                            for (int b = 0; b < Bpp; b++)
                                pRes[offset + b] = (byte)((pTile[offset + b] + pBg[offset + b]) >> 1);
                        }
                    }
                }
            }
            return result;
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
                int x = (offset % Stride) / Bpp;

                if (top && y == 0)
                    touchPixCount++;

                if (bottom && y == Height - 1)
                    touchPixCount++;

                if (left && x == 0)
                    touchPixCount++;

                if (right && x == Width - 1)
                    touchPixCount++;

                if (touchPixCount > 1)
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
