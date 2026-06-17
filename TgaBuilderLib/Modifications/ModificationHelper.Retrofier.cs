using System.Collections.Generic;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Modifications;

public partial class ModificationsHelper
{
    // =====================================================================
    // Stage 8 — Texture Retrofier (TR1 / TR2 Sega Saturn look)
    //
    // Recreates the limited colour space of early Tomb Raider textures:
    //   1. Optional ordered dithering (checkerboard / Bayer) that perturbs
    //      pixels before quantization to break up colour banding — or, when
    //      no quantization is active, as a standalone checker art effect.
    //   2. Per-channel quantization (6/5/4-bit) so RGB values become
    //      multiples of 4 / 8 / 16 (255 is preserved through clamping).
    //   3. Optional palette limitation (median cut) capping the texture to a
    //      maximum number of distinct colours, like the small per-texture
    //      palettes the original games used.
    // Alpha is never modified.
    // =====================================================================

    // 4x4 / 8x8 Bayer ordered-dithering threshold matrices.
    private static readonly int[] Bayer4 =
    {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };

    private static readonly int[] Bayer8 =
    {
         0, 32,  8, 40,  2, 34, 10, 42,
        48, 16, 56, 24, 50, 18, 58, 26,
        12, 44,  4, 36, 14, 46,  6, 38,
        60, 28, 52, 20, 62, 30, 54, 22,
         3, 35, 11, 43,  1, 33,  9, 41,
        51, 19, 59, 27, 49, 17, 57, 25,
        15, 47,  7, 39, 13, 45,  5, 37,
        63, 31, 55, 23, 61, 29, 53, 21
    };

    private void ApplyRetrofier(byte[] pixels)
    {
        int step = QuantStep(RetroQuantization);
        bool doQuantize = step > 1;
        bool doDither = RetroDitherMode != RetroDitherMode.None
            && RetroDitherStrength > 0.0001f;
        bool doPalette = RetroPaletteLimitEnabled;

        if (!doQuantize && !doDither && !doPalette)
            return;

        int w = Width;
        int h = Height;

        if (pixels.Length < w * h * BPP)
            return;

        if (doQuantize || doDither)
            ApplyRetroQuantizeDither(pixels, w, h, step, doQuantize, doDither);

        if (doPalette)
            ApplyRetroPalette(
                pixels,
                w,
                h,
                Math.Clamp(RetroMaxColors, RETRO_MIN_COLORS, RETRO_MAX_COLORS),
                step,
                doQuantize);
    }

    private static int QuantStep(RetroQuantizationLevel level) => level switch
    {
        RetroQuantizationLevel.SixBit => 4,
        RetroQuantizationLevel.FiveBit => 8,
        RetroQuantizationLevel.FourBit => 16,
        _ => 1
    };

    // ---------------------------------------------------------------------
    // Ordered dithering + per-channel quantization
    // ---------------------------------------------------------------------
    private void ApplyRetroQuantizeDither(
        byte[] pixels, int w, int h, int step, bool doQuantize, bool doDither)
    {
        float strength = doDither ? Math.Clamp(RetroDitherStrength, 0f, 1f) : 0f;
        int cell = Math.Clamp(RetroDitherCellSize, 1, RETRO_DITHER_CELL_SIZE_MAX);

        // When quantizing, the dither spans exactly one quantization interval
        // (textbook ordered dithering). Without quantization it acts as a
        // standalone checker effect with a fixed, visible amplitude.
        float amplitude = doQuantize ? step : RETRO_DITHER_FREE_AMPLITUDE;
        var mode = RetroDitherMode;

        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w * BPP;
            for (int x = 0; x < w; x++)
            {
                int i = rowBase + x * BPP;

                float offset = doDither
                    ? DitherThreshold(mode, x, y, cell) * strength * amplitude
                    : 0f;

                pixels[i + 0] = QuantizeChannel(pixels[i + 0], offset, step, doQuantize);
                pixels[i + 1] = QuantizeChannel(pixels[i + 1], offset, step, doQuantize);
                pixels[i + 2] = QuantizeChannel(pixels[i + 2], offset, step, doQuantize);
            }
        }
    }

    private static byte QuantizeChannel(byte v, float offset, int step, bool doQuantize)
    {
        float fv = v + offset;
        int iv = doQuantize
            ? (int)MathF.Round(fv / step) * step
            : (int)MathF.Round(fv);

        if (iv < 0) iv = 0;
        else if (iv > 255) iv = 255;

        return (byte)iv;
    }

    // Threshold in the range [-0.5, 0.5] for the requested pattern / cell size.
    private static float DitherThreshold(RetroDitherMode mode, int x, int y, int cell)
    {
        int cx = x / cell;
        int cy = y / cell;

        switch (mode)
        {
            case RetroDitherMode.Checkerboard:
                return ((cx + cy) & 1) == 0 ? -0.5f : 0.5f;
            case RetroDitherMode.Bayer4x4:
                return (Bayer4[(cy & 3) * 4 + (cx & 3)] + 0.5f) / 16f - 0.5f;
            case RetroDitherMode.Bayer8x8:
                return (Bayer8[(cy & 7) * 8 + (cx & 7)] + 0.5f) / 64f - 0.5f;
            default:
                return 0f;
        }
    }

    // ---------------------------------------------------------------------
    // Palette limitation via median cut
    // ---------------------------------------------------------------------
    private void ApplyRetroPalette(
        byte[] pixels, int w, int h, int maxColors, int step, bool snapToGrid)
    {
        int len = w * h * BPP;

        // 1. Histogram of distinct RGB colours.
        var histogram = new Dictionary<int, int>();
        for (int i = 0; i < len; i += BPP)
        {
            int key = (pixels[i + 2] << 16) | (pixels[i + 1] << 8) | pixels[i + 0];
            histogram.TryGetValue(key, out int c);
            histogram[key] = c + 1;
        }

        if (histogram.Count <= maxColors)
            return; // already within the limit

        int n = histogram.Count;
        byte[] cr = new byte[n];
        byte[] cg = new byte[n];
        byte[] cb = new byte[n];
        int[] cw = new int[n];

        int idx = 0;
        foreach (var kv in histogram)
        {
            cr[idx] = (byte)((kv.Key >> 16) & 0xFF);
            cg[idx] = (byte)((kv.Key >> 8) & 0xFF);
            cb[idx] = (byte)(kv.Key & 0xFF);
            cw[idx] = kv.Value;
            idx++;
        }

        // 2. Median cut: boxes are ranges into a reorderable index array.
        int[] order = new int[n];
        for (int i = 0; i < n; i++)
            order[i] = i;

        var boxes = new List<(int Start, int Count)> { (0, n) };

        while (boxes.Count < maxColors)
        {
            int bestBox = -1;
            int bestRange = 0;
            int bestAxis = 0;

            for (int bi = 0; bi < boxes.Count; bi++)
            {
                var (s, c) = boxes[bi];
                if (c <= 1)
                    continue;

                byte rmin = 255, rmax = 0, gmin = 255, gmax = 0, bmin = 255, bmax = 0;
                for (int k = s; k < s + c; k++)
                {
                    int ci = order[k];
                    if (cr[ci] < rmin) rmin = cr[ci];
                    if (cr[ci] > rmax) rmax = cr[ci];
                    if (cg[ci] < gmin) gmin = cg[ci];
                    if (cg[ci] > gmax) gmax = cg[ci];
                    if (cb[ci] < bmin) bmin = cb[ci];
                    if (cb[ci] > bmax) bmax = cb[ci];
                }

                int rr = rmax - rmin;
                int gr = gmax - gmin;
                int br = bmax - bmin;

                int axis = 0;
                int range = rr;
                if (gr > range) { range = gr; axis = 1; }
                if (br > range) { range = br; axis = 2; }

                if (range > bestRange)
                {
                    bestRange = range;
                    bestBox = bi;
                    bestAxis = axis;
                }
            }

            if (bestBox < 0)
                break; // nothing left to split

            var (bs, bc) = boxes[bestBox];
            int axisSel = bestAxis;

            Array.Sort(order, bs, bc, Comparer<int>.Create((a, b2) =>
            {
                int va = axisSel == 0 ? cr[a] : axisSel == 1 ? cg[a] : cb[a];
                int vb = axisSel == 0 ? cr[b2] : axisSel == 1 ? cg[b2] : cb[b2];
                return va - vb;
            }));

            // Split at the population-weighted median.
            long total = 0;
            for (int k = bs; k < bs + bc; k++)
                total += cw[order[k]];

            long acc = 0;
            int splitAt = bs + 1;
            for (int k = bs; k < bs + bc; k++)
            {
                acc += cw[order[k]];
                if (acc * 2 >= total)
                {
                    splitAt = k + 1;
                    break;
                }
            }

            if (splitAt <= bs) splitAt = bs + 1;
            if (splitAt >= bs + bc) splitAt = bs + bc - 1;

            boxes[bestBox] = (bs, splitAt - bs);
            boxes.Add((splitAt, bs + bc - splitAt));
        }

        // 3. Representative colour per box (population-weighted average).
        int pn = boxes.Count;
        byte[] pr = new byte[pn];
        byte[] pg = new byte[pn];
        byte[] pb = new byte[pn];

        for (int bi = 0; bi < pn; bi++)
        {
            var (s, c) = boxes[bi];
            long sr = 0, sg = 0, sb = 0, sw = 0;
            for (int k = s; k < s + c; k++)
            {
                int ci = order[k];
                sr += (long)cr[ci] * cw[ci];
                sg += (long)cg[ci] * cw[ci];
                sb += (long)cb[ci] * cw[ci];
                sw += cw[ci];
            }

            byte rr = sw > 0 ? (byte)(sr / sw) : (byte)0;
            byte gg = sw > 0 ? (byte)(sg / sw) : (byte)0;
            byte bb = sw > 0 ? (byte)(sb / sw) : (byte)0;

            if (snapToGrid)
            {
                rr = SnapToStep(rr, step);
                gg = SnapToStep(gg, step);
                bb = SnapToStep(bb, step);
            }

            pr[bi] = rr;
            pg[bi] = gg;
            pb[bi] = bb;
        }

        // 4. Map every pixel to its nearest palette colour (cached per colour).
        var mapCache = new Dictionary<int, int>(pn);
        for (int i = 0; i < len; i += BPP)
        {
            int key = (pixels[i + 2] << 16) | (pixels[i + 1] << 8) | pixels[i + 0];

            if (!mapCache.TryGetValue(key, out int pi))
            {
                int rr = pixels[i + 2];
                int gg = pixels[i + 1];
                int bb = pixels[i + 0];

                int best = 0;
                long bestD = long.MaxValue;
                for (int q = 0; q < pn; q++)
                {
                    long dr = rr - pr[q];
                    long dg = gg - pg[q];
                    long db = bb - pb[q];
                    long d = dr * dr + dg * dg + db * db;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = q;
                    }
                }

                pi = best;
                mapCache[key] = pi;
            }

            pixels[i + 0] = pb[pi];
            pixels[i + 1] = pg[pi];
            pixels[i + 2] = pr[pi];
        }
    }

    private static byte SnapToStep(byte v, int step)
    {
        if (step <= 1)
            return v;

        int q = (int)MathF.Round((float)v / step) * step;
        if (q > 255) q = 255;
        return (byte)q;
    }
}
