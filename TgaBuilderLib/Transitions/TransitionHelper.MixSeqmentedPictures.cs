using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TgaBuilderLib.Transitions
{
      partial class TransitionHelper
    {
                    // Draws segmented tile pixels over a background according to transition progress and edge constraints.
          public byte[] MixSmartTilesPixels(
            byte[] bgPixels,
            byte[] tilePixels)
        {
            if (bgPixels.Length != tilePixels.Length)
                throw new ArgumentException("Pixel arrays must have same length.");

            bool[] isLabelDrawn = new bool[TileData.Count + 1];
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

                    for (int i = 0; i < TileData.Count; i++)
                    {
                        var tile = TileData[i];
                        int labelID = i + 1;

                        // 1. Pivot condition (v is the computed progress value of this tile)
                        float v = ComputeFocusV(Mode, tile);
                        bool shouldDraw = ReversePivot ? (v <= Pivot) : (v >= Pivot);

                        // 2. Edge condition (check edge tiles)

                        if(shouldDraw)
                        {
                            shouldDraw = !DoesTileTouchRequiredEdge(tile, !checkTop, !checkBottom, !checkLeft, !checkRight);
                        }

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
                        if (y > 0 && !isLabelDrawn[Labels[(y - 1) * Width + x]]) 
                            backgroundNeighbors++;

                        if (y < Height - 1 && !isLabelDrawn[Labels[(y + 1) * Width + x]]) 
                            backgroundNeighbors++;

                        if (x > 0 && !isLabelDrawn[Labels[y * Width + (x - 1)]]) 
                            backgroundNeighbors++;

                        if (x < Width - 1 && !isLabelDrawn[Labels[y * Width + (x + 1)]]) 
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
        private  (bool checkTop, bool checkBottom, bool checkLeft, bool checkRight) GetDrawnEdgeTilesBools(
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
        private  bool DoesTileTouchRequiredEdge(TileSegment tile, bool top, bool bottom, bool left, bool right)
        {
            foreach (int offset in tile.PixelOffsets)
            {
                int y = offset / Stride;
                int x = (offset % Stride) / Bpp;

                if (top && y == 0) return true;
                if (bottom && y == Height - 1) return true;
                if (left && x == 0) return true;
                if (right && x == Width - 1) return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Computes a normalized focus value for a tile based on its centroid.
        private  float ComputeFocusV(TransitionMode mode, TileSegment tile)
        {
            float nx = tile.CentroidX;
            float ny = tile.CentroidY;

            // --- Topological logic excerpt ---
            float distToT1 = 0, distToT2 = 0;

            (distToT1, distToT2) = ComputeTopologicalLogic(mode, nx, ny);

            float v;
            if (distToT2 <= 0.00001f) v = 1.0f;
            else if (distToT1 <= 0.00001f) v = 0.0f;
            else v = distToT1 / (distToT1 + distToT2);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Computes directional distances to texture domains for the selected transition mode.
        private  (float distToT1, float distToT2) ComputeTopologicalLogic(TransitionMode mode, float nx, float ny)
            => mode switch
            {
                TransitionMode.Top => (
                    Math.Min(nx, Math.Min(1.0f - nx, 1.0f - ny)),
                    ny
                ),

                TransitionMode.Bottom => (
                    Math.Min(nx, Math.Min(1.0f - nx, ny)),
                    1.0f - ny
                ),

                TransitionMode.Left => (
                    Math.Min(ny, Math.Min(1.0f - ny, 1.0f - nx)),
                    nx
                ),

                TransitionMode.Right => (
                    Math.Min(ny, Math.Min(1.0f - ny, nx)),
                    1.0f - nx
                ),

                TransitionMode.DiagonalTopLeft => (
                    Math.Min(1.0f - nx, 1.0f - ny),
                    Math.Min(nx, ny)
                ),

                TransitionMode.DiagonalTopRight => (
                    Math.Min(nx, 1.0f - ny),
                    Math.Min(1.0f - nx, ny)
                ),

                _ => (0f, 0f)
            };

    }
}
