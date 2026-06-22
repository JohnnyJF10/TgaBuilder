using System.Runtime.CompilerServices;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{

    public enum EdgeBlendMode
    {
        Multiply,       // Multiply (Darkens, like a glaze)
        Screen,         // Multiply negatively (Lightens, like projection)
        Additive,       // Add (Extremely lightens, glow effect)
        Overlay,        // Copy into each other (Enhances contrast)
        HardLight,      // Hard light (Strong effect)
        SoftLight,      // Soft light (Smooth contrast effect)
        ColorDodge,     // Brightens strongly with edge color
        ColorBurn       // Darkens strongly with edge color
    }

    // Writes the blended transition into the caller-provided result buffer using the reusable
    // _scratchShadowedBg buffer for the intermediate shadow pass. Both buffers are fully
    // overwritten via Buffer.MemoryCopy before any per-pixel work, so reuse leaves no stale data.
    private void BricksDraw(byte[] tilePixels, byte[] bgPixels, bool[] selection, byte[] result)
    {
        if (bgPixels.Length != tilePixels.Length)
            throw new ArgumentException("Input image raw arrays must have same length.");

        if (bgPixels.Length != selection.Length * TRANSITIONS_BPP)
            throw new ArgumentException("Input arrays length must match dimensions.");

        // Clamp the maximum edge width to the range 0 to 12
        EdgeWidth = Math.Clamp(EdgeWidth, 0, 12);
        ShadowSize = Math.Clamp(ShadowSize, 0, 32);
        ShadowHardness = Math.Clamp(ShadowHardness, 0, 100);

        byte[] shadowedBg = _scratchShadowedBg;

        // Precompute the pixel-to-edge distance map once and reuse it for both passes. It depends
        // only on the selection, so drawing-only recalcs (edge/shadow slider changes) reuse it.
        if (!_edgeDistValid)
        {
            ComputeEdgeDistanceTransform(selection, _edgeDist);
            _edgeDistValid = true;
        }

        DrawShadows(bgPixels, selection, _edgeDist, shadowedBg);

        DrawResult(tilePixels, selection, _edgeDist, shadowedBg, result);

        // Manually moved/rotated tiles are part of the selection (so they cast shadows and form
        // the mask correctly), but the base pass above sampled the static tile image at their new
        // positions, i.e. the wrong content. Repaint their true content from the original source
        // pixels here, on top of everything, so they overlay any static tiles they now cover.
        DrawManipulatedTiles(tilePixels, result);
    }

    // Repaints the user-manipulated tiles over the finished base result. For each destination pixel
    // the true source color (the tile's original, un-rotated image data) is blended through the
    // shared BlendSelectedPixel path, so moved/rotated tiles get the same edge tinting (and over the
    // same shadowed background) as static tiles. Edge proximity uses _edgeDist, which was computed
    // from the final selection that already includes these footprints. Drawn in list order with the
    // active tile last, so the tile the user is currently manipulating ends up on top.
    private void DrawManipulatedTiles(byte[] tilePixels, byte[] result)
    {
        if (_manipulatedTiles.Count == 0)
            return;

        int eA = EdgeColor.A ?? 255;
        int invA = 255 - eA;
        int eR = EdgeColor.R;
        int eG = EdgeColor.G;
        int eB = EdgeColor.B;

        unsafe
        {
            fixed (byte* pTile = tilePixels)
            fixed (byte* pRes = result)
            fixed (byte* pShadowBg = _scratchShadowedBg)
            fixed (int* pEdgeDist = _edgeDist)
            {
                foreach (var tile in _manipulatedTiles)
                {
                    int[] dst = tile.DstOffsets;
                    int[] src = tile.SrcOffsets;
                    for (int i = 0; i < dst.Length; i++)
                    {
                        int d = dst[i];
                        int s = src[i];
                        int x = d % Width;
                        int y = d / Width;

                        BlendSelectedPixel(
                            pRes, d * TRANSITIONS_BPP,
                            pTile, s * TRANSITIONS_BPP,
                            pShadowBg, d * TRANSITIONS_BPP,
                            x, y, pEdgeDist[d],
                            eA, invA, eR, eG, eB);
                    }
                }
            }
        }
    }

    // Computes, for every pixel, the Chebyshev (L-infinity) distance to the nearest pixel of
    // OPPOSITE selection state (min value 1; the INF sentinel when no opposite pixel exists).
    // This is the same quantity the per-pixel ring searches in DrawShadows/DrawResult used to
    // compute, but in O(Width*Height) via an exact two-pass (1,1) chamfer distance transform.
    // The image border is intentionally NOT treated as a boundary: out-of-image neighbors are
    // skipped, so border falloff stays driven solely by the dynamicSize cap in the draw passes
    // (this mirrors the old out-of-bounds branch, which was dead code under that cap).
    private void ComputeEdgeDistanceTransform(bool[] selection, int[] edgeDist)
    {
        int width = Width;
        int height = Height;
        int inf = width + height;

        unsafe
        {
            fixed (bool* pSel = selection)
            fixed (int* pDist = edgeDist)
            {
                // Seed pass: a pixel with an 8-connected neighbor of opposite state is distance 1
                // from the boundary (D == 1, the global minimum); everything else starts at INF.
                // Seeding with 1 (rather than 0) lets the two passes below converge directly to
                // D(i) = distance to nearest opposite pixel, with no separate +1 fold.
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int i = row + x;
                        bool s = pSel[i];

                        bool boundary = false;
                        int yStart = y > 0 ? -1 : 0;
                        int yEnd = y < height - 1 ? 1 : 0;
                        int xStart = x > 0 ? -1 : 0;
                        int xEnd = x < width - 1 ? 1 : 0;

                        for (int dy = yStart; dy <= yEnd && !boundary; dy++)
                        {
                            int nRow = i + dy * width;
                            for (int dx = xStart; dx <= xEnd; dx++)
                            {
                                if (dx == 0 && dy == 0)
                                    continue;
                                if (pSel[nRow + dx] != s)
                                {
                                    boundary = true;
                                    break;
                                }
                            }
                        }

                        pDist[i] = boundary ? 1 : inf;
                    }
                }

                // Forward pass: relax against NW, N, NE, W neighbors (finalized in raster order).
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int i = row + x;
                        int best = pDist[i];
                        if (best == 1)
                            continue; // already at the global minimum

                        if (y > 0)
                        {
                            int up = i - width;
                            best = MinPlusOne(best, pDist[up], inf);           // N
                            if (x > 0) best = MinPlusOne(best, pDist[up - 1], inf);          // NW
                            if (x < width - 1) best = MinPlusOne(best, pDist[up + 1], inf);  // NE
                        }
                        if (x > 0) best = MinPlusOne(best, pDist[i - 1], inf);  // W

                        pDist[i] = best;
                    }
                }

                // Backward pass: relax against SE, S, SW, E neighbors (finalized in reverse order).
                for (int y = height - 1; y >= 0; y--)
                {
                    int row = y * width;
                    for (int x = width - 1; x >= 0; x--)
                    {
                        int i = row + x;
                        int best = pDist[i];
                        if (best == 1)
                            continue;

                        if (y < height - 1)
                        {
                            int down = i + width;
                            best = MinPlusOne(best, pDist[down], inf);          // S
                            if (x < width - 1) best = MinPlusOne(best, pDist[down + 1], inf); // SE
                            if (x > 0) best = MinPlusOne(best, pDist[down - 1], inf);         // SW
                        }
                        if (x < width - 1) best = MinPlusOne(best, pDist[i + 1], inf); // E

                        pDist[i] = best;
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Returns min(current, neighbor + 1), treating the INF sentinel as unreachable (no overflow).
    private static int MinPlusOne(int current, int neighbor, int inf)
    {
        if (neighbor >= inf)
            return current;
        int candidate = neighbor + 1;
        return candidate < current ? candidate : current;
    }

    private void DrawShadows(byte[] bgPixels, bool[] selection, int[] edgeDist, byte[] shadowedBg)
    {
        unsafe
        {
            if (bgPixels.Length != shadowedBg.Length)
                throw new ArgumentException("Background and shadow buffer must have the same length.");

            if (bgPixels.Length != selection.Length * TRANSITIONS_BPP)
                throw new ArgumentException("Input arrays length must match dimensions.");

            int stride = Width * TRANSITIONS_BPP;

            fixed (byte* pBg = bgPixels)
            fixed (byte* pShadowBg = shadowedBg)
            {
                // Copy the background first for dedicated shadow rendering.
                Buffer.MemoryCopy(pBg, pShadowBg, Height * stride, Height * stride);

                int sA = ShadowColor.A ?? 255;
                int sR = ShadowColor.R;
                int sG = ShadowColor.G;
                int sB = ShadowColor.B;

                // Assuming ShadowHardness is stored as a percentage (0-100).
                // If your property is already normalized between 0.0 and 1.0, 
                // you can remove the `/ 100.0`.
                double hardness = Math.Clamp(ShadowHardness / 100.0, 0.0, 1.0);

                // Pass 1: render shadows on top of the background-only buffer.
                for (int y = 0; y < Height; y++)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < Width; x++)
                    {
                        int pixelIndex = y * Width + x;
                        int offset = rowOffset + (x * 4);

                        int distToBorderX = Math.Min(x, Width - 1 - x);
                        int distToBorderY = Math.Min(y, Height - 1 - y);
                        int distToBorder = Math.Min(distToBorderX, distToBorderY);
                        int dynamicShadowSize = Math.Min(ShadowSize, distToBorder);

                        if (dynamicShadowSize <= 0)
                            continue;

                        // Distance to the nearest opposite-selection pixel (precomputed).
                        int minOppositeDist = edgeDist[pixelIndex];

                        if (minOppositeDist <= dynamicShadowSize)
                        {
                            int borderDist = minOppositeDist - 1;
                            double proximity = (double)(dynamicShadowSize - borderDist) / dynamicShadowSize;

                            // Interploate based on hardness: 
                            // 0.0 -> gradient reduces linearly to 0
                            // 1.0 -> adjustedProximity stays locked at 1.0
                            // In-between -> flatter gradient that drops sharply to 0 out of bounds
                            double adjustedProximity = proximity + hardness * (1.0 - proximity);

                            int shadowWeight255 = (int)Math.Clamp(Math.Round(adjustedProximity * sA), 0, 255);
                            int invShadowWeight255 = 255 - shadowWeight255;

                            pShadowBg[offset + 0] = (byte)((sB * shadowWeight255 + pBg[offset + 0] * invShadowWeight255) / 255);
                            pShadowBg[offset + 1] = (byte)((sG * shadowWeight255 + pBg[offset + 1] * invShadowWeight255) / 255);
                            pShadowBg[offset + 2] = (byte)((sR * shadowWeight255 + pBg[offset + 2] * invShadowWeight255) / 255);
                            pShadowBg[offset + 3] = (byte)((255 * shadowWeight255 + pBg[offset + 3] * invShadowWeight255) / 255);
                        }
                    }
                }
            }
        }
    }

    private void DrawResult(byte[] tilePixels, bool[] selection, int[] edgeDist, byte[] shadowedBg, byte[] result)
    {
        if (tilePixels.Length != shadowedBg.Length)
            throw new ArgumentException("Tile and shadow buffer must have the same length.");

        if (tilePixels.Length != selection.Length * TRANSITIONS_BPP)
            throw new ArgumentException("Input arrays length must match dimensions.");

        if (tilePixels.Length != result.Length)
            throw new ArgumentException("Tile and result buffers must have the same length.");

        int stride = Width * TRANSITIONS_BPP;

        unsafe
        {
            fixed (byte* pTile = tilePixels)
            fixed (byte* pShadowBg = shadowedBg)
            fixed (byte* pRes = result)
            {



                // Pass 2 base: start from shadowed background.
                Buffer.MemoryCopy(pShadowBg, pRes, Height * stride, Height * stride);

                // Pre-calculate alpha and color values for basic edge blending
                int eA = EdgeColor.A ?? 255;      // Edge alpha (0-255)
                int invA = 255 - eA;              // Inverse alpha for background contribution
                int eR = EdgeColor.R;
                int eG = EdgeColor.G;
                int eB = EdgeColor.B;

                // Pass 3: existing tile/edge rendering over the (already) shadowed background.
                for (int y = 0; y < Height; y++)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < Width; x++)
                    {
                        int pixelIndex = y * Width + x;
                        if (!selection[pixelIndex])
                            continue;

                        int offset = rowOffset + (x * 4);

                        // Selected tile pixel: copy the tile color, tinting it toward EdgeColor near
                        // the selection boundary. Shared with the manipulated-tile overlay pass so
                        // both static and moved/rotated tiles get identical edge tinting.
                        BlendSelectedPixel(
                            pRes, offset,
                            pTile, offset,
                            pShadowBg, offset,
                            x, y, edgeDist[pixelIndex],
                            eA, invA, eR, eG, eB);
                    }
                }
            }
        }
    }

    // Writes one selected tile pixel into the result: the tile color, blended toward EdgeColor
    // (per BlendMode and EdgeWidth) as it approaches the selection boundary, over the shadowed
    // background. Factored out of DrawResult so the manipulated-tile overlay reuses the exact same
    // edge tinting. AggressiveInlining keeps DrawResult's per-pixel hot loop allocation/call-free.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void BlendSelectedPixel(
        byte* pRes, int resOffset,
        byte* pTile, int tileOffset,
        byte* pShadowBg, int shadowOffset,
        int x, int y, int minDist,
        int eA, int invA, int eR, int eG, int eB)
    {
        // Dynamic edge width drops linearly towards the image bounds.
        int distToBorderX = Math.Min(x, Width - 1 - x);
        int distToBorderY = Math.Min(y, Height - 1 - y);
        int distToBorder = Math.Min(distToBorderX, distToBorderY);
        int dynamicEdgeWidth = Math.Min(EdgeWidth, distToBorder);

        if (dynamicEdgeWidth > 0 && minDist <= dynamicEdgeWidth)
        {
            int weight255 = ((dynamicEdgeWidth - minDist + 1) * 255) / dynamicEdgeWidth;
            int invWeight255 = 255 - weight255;

            // Current channels of the tile pixel
            int tB = pTile[tileOffset + 0];
            int tG = pTile[tileOffset + 1];
            int tR = pTile[tileOffset + 2];
            int tA = pTile[tileOffset + 3];

            int tintedB, tintedG, tintedR;

            // 1. Application mode for the edge color
            switch (BlendMode)
            {
                case EdgeBlendMode.Screen: // Multiply negatively
                    tintedB = 255 - ((255 - tB) * (255 - eB) / 255);
                    tintedG = 255 - ((255 - tG) * (255 - eG) / 255);
                    tintedR = 255 - ((255 - tR) * (255 - eR) / 255);
                    break;

                case EdgeBlendMode.Additive: // Add
                    tintedB = Math.Min(255, tB + eB);
                    tintedG = Math.Min(255, tG + eG);
                    tintedR = Math.Min(255, tR + eR);
                    break;

                case EdgeBlendMode.Overlay: // Copy into each other
                    tintedB = (tB < 128) ? (2 * tB * eB / 255) : (255 - 2 * (255 - tB) * (255 - eB) / 255);
                    tintedG = (tG < 128) ? (2 * tG * eG / 255) : (255 - 2 * (255 - tG) * (255 - eG) / 255);
                    tintedR = (tR < 128) ? (2 * tR * eR / 255) : (255 - 2 * (255 - tR) * (255 - eR) / 255);
                    break;

                case EdgeBlendMode.HardLight:
                    tintedB = (eB < 128) ? (2 * tB * eB / 255) : (255 - 2 * (255 - tB) * (255 - eB) / 255);
                    tintedG = (eG < 128) ? (2 * tG * eG / 255) : (255 - 2 * (255 - tG) * (255 - eG) / 255);
                    tintedR = (eR < 128) ? (2 * tR * eR / 255) : (255 - 2 * (255 - tR) * (255 - eR) / 255);
                    break;

                case EdgeBlendMode.SoftLight:
                    tintedB = SoftLightChannel(tB, eB);
                    tintedG = SoftLightChannel(tG, eG);
                    tintedR = SoftLightChannel(tR, eR);
                    break;

                case EdgeBlendMode.ColorDodge:
                    tintedB = eB == 255 ? 255 : Math.Min(255, (tB * 255) / (255 - eB));
                    tintedG = eG == 255 ? 255 : Math.Min(255, (tG * 255) / (255 - eG));
                    tintedR = eR == 255 ? 255 : Math.Min(255, (tR * 255) / (255 - eR));
                    break;

                case EdgeBlendMode.ColorBurn:
                    tintedB = eB == 0 ? 0 : Math.Max(0, 255 - ((255 - tB) * 255) / eB);
                    tintedG = eG == 0 ? 0 : Math.Max(0, 255 - ((255 - tG) * 255) / eG);
                    tintedR = eR == 0 ? 0 : Math.Max(0, 255 - ((255 - tR) * 255) / eR);
                    break;

                case EdgeBlendMode.Multiply: // Standard: Multiply
                default:
                    tintedB = (tB * eB) / 255;
                    tintedG = (tG * eG) / 255;
                    tintedR = (tR * eR) / 255;
                    break;
            }

            // 2. Background influence: how strongly the edge color influences the background.
            int maxEdgeB = (tintedB * eA + pShadowBg[shadowOffset + 0] * invA) / 255;
            int maxEdgeG = (tintedG * eA + pShadowBg[shadowOffset + 1] * invA) / 255;
            int maxEdgeR = (tintedR * eA + pShadowBg[shadowOffset + 2] * invA) / 255;
            int maxEdgeAlpha = (tA * eA + pShadowBg[shadowOffset + 3] * invA) / 255;

            // 3. Final gradient blending based on distance to edge
            pRes[resOffset + 0] = (byte)((maxEdgeB * weight255 + tB * invWeight255) / 255);
            pRes[resOffset + 1] = (byte)((maxEdgeG * weight255 + tG * invWeight255) / 255);
            pRes[resOffset + 2] = (byte)((maxEdgeR * weight255 + tR * invWeight255) / 255);
            pRes[resOffset + 3] = (byte)((maxEdgeAlpha * weight255 + tA * invWeight255) / 255);
        }
        else
        {
            // Inner pixels or absolute image border pixels: copy original tile.
            pRes[resOffset + 0] = pTile[tileOffset + 0];
            pRes[resOffset + 1] = pTile[tileOffset + 1];
            pRes[resOffset + 2] = pTile[tileOffset + 2];
            pRes[resOffset + 3] = pTile[tileOffset + 3];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SoftLightChannel(int tileChannel, int edgeChannel)
    {
        double tileNorm = tileChannel / 255.0;
        double edgeNorm = edgeChannel / 255.0;
        double blended = ((1.0 - (2.0 * edgeNorm)) * tileNorm * tileNorm) + (2.0 * edgeNorm * tileNorm);
        return (int)Math.Clamp(blended * 255.0, 0, 255);
    }
}
