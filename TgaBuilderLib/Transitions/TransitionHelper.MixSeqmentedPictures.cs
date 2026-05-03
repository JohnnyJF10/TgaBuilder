using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Net.WebRequestMethods;

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

            var currentCentroids = new (float X, float Y)[Centroids.Length];
            Array.Copy(Centroids, currentCentroids, Centroids.Length);

            var currentLabels = new int[Labels.Length];
            Array.Copy(Labels, currentLabels, Labels.Length);

            if (currentLabels.Length > 0 && currentLabels.Max() > currentCentroids.Length)
                return bgPixels; // Fallback in case of race condition

            // Determine relevant edges based on mode and reverse pivot
            (bool checkTop, bool checkBottom, bool checkLeft, bool checkRight) =
                GetDrawnEdgeTilesBools(Mode, ReversePivot);

            // Selection step: determine which pixels are drawn (Input → Label Map → Selection)
            bool[] selection = BuildSelection(currentCentroids, currentLabels,
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
            (float X, float Y)[] centroids,
            int[] labels,
            bool checkTop, bool checkBottom, bool checkLeft, bool checkRight)
        {
            bool[] selection = new bool[Width * Height];
            int labelCount = centroids.Length;

            // --- STEP 1: Fast Grouping of Offsets (CSR Approach) ---
            int[] counts = new int[labelCount + 1];
            for (int i = 0; i < labels.Length; i++)
            {
                int lbl = labels[i];
                if (lbl > 0 && lbl <= labelCount) counts[lbl]++;
            }

            int[] startIndices = new int[labelCount + 2];
            int currentPos = 0;
            for (int i = 1; i <= labelCount; i++)
            {
                startIndices[i] = currentPos;
                currentPos += counts[i];
            }
            startIndices[labelCount + 1] = currentPos;

            int[] flatOffsets = new int[currentPos];
            int[] writePos = (int[])startIndices.Clone();

            for (int i = 0; i < labels.Length; i++)
            {
                int lbl = labels[i];
                if (lbl > 0 && lbl <= labelCount)
                {
                    flatOffsets[writePos[lbl]++] = i;
                }
            }

            // --- STEP 2: Selection Logic ---
            var cornerTileMap = SliceCornerTiles
                ? BuildCornerTileMap(labels, checkTop, checkBottom, checkLeft, checkRight)
                : null;

            for (int i = 0; i < labelCount; i++)
            {
                int labelID = i + 1;
                int start = startIndices[labelID];
                int end = startIndices[labelID + 1];

                // tileOffsets now contains PIXEL indices (0 to Width*Height)
                ReadOnlySpan<int> tileOffsets = new ReadOnlySpan<int>(flatOffsets, start, end - start);
                if (tileOffsets.Length == 0) continue;

                var centroid = centroids[i];
                float v = ComputeFocusV(Mode, centroid);
                bool shouldDraw = ReversePivot ? (v <= Pivot) : (v >= Pivot);

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
                bool thisVert  = (cornX == 0 && checkLeft) || (cornX == Width - 1 && checkRight);

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
                (TransitionMode.Top, false)                 => (true, false, false, false),
                (TransitionMode.Top, true)                  => (false, true, true, true),
                (TransitionMode.Bottom, false)              => (false, true, false, false),
                (TransitionMode.Bottom, true)               => (true, false, true, true),
                (TransitionMode.Left, false)                => (false, false, true, false),
                (TransitionMode.Left, true)                 => (true, true, false, true),
                (TransitionMode.Right, false)               => (false, false, false, true),
                (TransitionMode.Right, true)                => (true, true, true, false),
                (TransitionMode.DiagonalTopLeft, false)     => (true, false, true, false),
                (TransitionMode.DiagonalTopLeft, true)      => (false, true, false, true),
                (TransitionMode.DiagonalTopRight, false)    => (true, false, false, true),
                (TransitionMode.DiagonalTopRight, true)     => (false, true, true, false),
                _ => throw new ArgumentException("Invalid mode.")
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Checks whether a tile touches any of the requested image edges.
        private bool DoesTileTouchRequiredEdge(ReadOnlySpan<int> pixelOffsets, bool top, bool bottom, bool left, bool right)
        {
            int touchPixCount = 0;

            foreach (int offset in pixelOffsets)
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
                TransitionMode.Top              => (Math.Min(nx, Math.Min(1.0f - nx, 1.0f - ny)), ny),
                TransitionMode.Bottom           => (Math.Min(nx, Math.Min(1.0f - nx, ny)), 1.0f - ny),
                TransitionMode.Left             => (Math.Min(ny, Math.Min(1.0f - ny, 1.0f - nx)), nx),
                TransitionMode.Right            => (Math.Min(ny, Math.Min(1.0f - ny, nx)), 1.0f - nx),
                TransitionMode.DiagonalTopLeft  => (Math.Min(1.0f - nx, 1.0f - ny), Math.Min(nx, ny)),
                TransitionMode.DiagonalTopRight => (Math.Min(nx, 1.0f - ny), Math.Min(1.0f - nx, ny)),
                _ => (0f, 0f)
            };
    }
}
