using TrLynxLib.Enums;

namespace TrLynxLib.Modifications;

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
    //
    // The palette step works entirely on the image-sized integer buffers
    // provisioned by EnsureBuffers: per-pixel packed-RGB keys are sorted so the
    // colour histogram is read off as runs of equal keys, and pixels are mapped
    // back to the palette with a binary search — no per-recalc allocations.
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
        int quantizationStep = QuantStep(RetroQuantization);
        bool applyQuantization = quantizationStep > 1;
        bool applyDithering = RetroDitherMode != RetroDitherMode.None
            && RetroDitherStrength > 0.0001f;
        bool applyPalette = RetroPaletteLimitEnabled;

        if (!applyQuantization && !applyDithering && !applyPalette)
            return;

        int width = Width;
        int height = Height;

        if (pixels.Length < width * height * BPP)
            return;

        if (applyQuantization || applyDithering)
            ApplyRetroQuantizeDither(pixels, width, height, quantizationStep, applyQuantization, applyDithering);

        if (applyPalette)
            ApplyRetroPalette(
                pixels,
                width,
                height,
                Math.Clamp(RetroMaxColors, RETRO_MIN_COLORS, RETRO_MAX_COLORS),
                quantizationStep,
                applyQuantization);
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
        byte[] pixels, int width, int height, int quantizationStep, bool applyQuantization, bool applyDithering)
    {
        float ditherStrength = applyDithering ? Math.Clamp(RetroDitherStrength, 0f, 1f) : 0f;
        int cellSize = Math.Clamp(RetroDitherCellSize, 1, RETRO_DITHER_CELL_SIZE_MAX);

        // When quantizing, the dither spans exactly one quantization interval
        // (textbook ordered dithering). Without quantization it acts as a
        // standalone checker effect with a fixed, visible amplitude.
        float ditherAmplitude = applyQuantization ? quantizationStep : RETRO_DITHER_FREE_AMPLITUDE;
        var ditherMode = RetroDitherMode;

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width * BPP;
            for (int x = 0; x < width; x++)
            {
                int i = rowStart + x * BPP;

                float ditherOffset = applyDithering
                    ? DitherThreshold(ditherMode, x, y, cellSize) * ditherStrength * ditherAmplitude
                    : 0f;

                pixels[i + 0] = QuantizeChannel(pixels[i + 0], ditherOffset, quantizationStep, applyQuantization);
                pixels[i + 1] = QuantizeChannel(pixels[i + 1], ditherOffset, quantizationStep, applyQuantization);
                pixels[i + 2] = QuantizeChannel(pixels[i + 2], ditherOffset, quantizationStep, applyQuantization);
            }
        }
    }

    private static byte QuantizeChannel(byte value, float ditherOffset, int quantizationStep, bool applyQuantization)
    {
        float dithered = value + ditherOffset;
        int quantized = applyQuantization
            ? (int)MathF.Round(dithered / quantizationStep) * quantizationStep
            : (int)MathF.Round(dithered);

        if (quantized < 0) quantized = 0;
        else if (quantized > 255) quantized = 255;

        return (byte)quantized;
    }

    // Threshold in the range [-0.5, 0.5] for the requested pattern / cell size.
    private static float DitherThreshold(RetroDitherMode ditherMode, int pixelX, int pixelY, int cellSize)
    {
        int cellX = pixelX / cellSize;
        int cellY = pixelY / cellSize;

        switch (ditherMode)
        {
            case RetroDitherMode.Checkerboard:
                return ((cellX + cellY) & 1) == 0 ? -0.5f : 0.5f;
            case RetroDitherMode.Bayer4x4:
                return (Bayer4[(cellY & 3) * 4 + (cellX & 3)] + 0.5f) / 16f - 0.5f;
            case RetroDitherMode.Bayer8x8:
                return (Bayer8[(cellY & 7) * 8 + (cellX & 7)] + 0.5f) / 64f - 0.5f;
            default:
                return 0f;
        }
    }

    // ---------------------------------------------------------------------
    // Palette limitation via median cut (one step per helper, on reusable buffers)
    // ---------------------------------------------------------------------
    private void ApplyRetroPalette(
        byte[] pixels, int width, int height, int maxColors, int quantizationStep, bool snapToGrid)
    {
        int pixelCount = width * height;

        if (pixels.Length < pixelCount * BPP || _retroKeys.Length < pixelCount)
            return;

        FillColorKeys(pixels, pixelCount);
        Array.Sort(_retroKeys, 0, pixelCount);

        int distinctCount = ExtractDistinctColors(pixelCount);
        if (distinctCount <= maxColors)
            return; // already within the limit

        MedianCut(distinctCount, maxColors);
        BuildPalette(quantizationStep, snapToGrid);
        RemapToPalette(pixels, pixelCount, distinctCount);
    }

    // Step 1 — packed RGB key (0x00RRGGBB) for every pixel.
    private void FillColorKeys(byte[] pixels, int pixelCount)
    {
        for (int p = 0, i = 0; p < pixelCount; p++, i += BPP)
            _retroKeys[p] = (pixels[i + 2] << 16) | (pixels[i + 1] << 8) | pixels[i + 0];
    }

    // Step 2 — read the histogram off the sorted keys as runs of equal values.
    // Returns the distinct-colour count; fills _retroDistinct (sorted) / _retroCounts.
    private int ExtractDistinctColors(int pixelCount)
    {
        int distinctCount = 0;
        int j = 0;
        while (j < pixelCount)
        {
            int key = _retroKeys[j];
            int run = 1;
            while (j + run < pixelCount && _retroKeys[j + run] == key)
                run++;

            _retroDistinct[distinctCount] = key;
            _retroCounts[distinctCount] = run;
            distinctCount++;
            j += run;
        }

        return distinctCount;
    }

    // Step 3 — median cut: boxes are ranges into the reorderable _retroOrder.
    private void MedianCut(int distinctCount, int maxColors)
    {
        for (int i = 0; i < distinctCount; i++)
            _retroOrder[i] = i;

        _retroBoxes.Clear();
        _retroBoxes.Add((0, distinctCount));

        while (_retroBoxes.Count < maxColors)
        {
            int targetBox = -1;
            int widestRange = 0;
            int splitAxis = 0;

            for (int boxIndex = 0; boxIndex < _retroBoxes.Count; boxIndex++)
            {
                var (boxStart, boxCount) = _retroBoxes[boxIndex];
                if (boxCount <= 1)
                    continue;

                int minR = 255, maxR = 0, minG = 255, maxG = 0, minB = 255, maxB = 0;
                for (int k = boxStart; k < boxStart + boxCount; k++)
                {
                    var (r, g, b) = UnpackColor(_retroDistinct[_retroOrder[k]]);
                    if (r < minR) minR = r;
                    if (r > maxR) maxR = r;
                    if (g < minG) minG = g;
                    if (g > maxG) maxG = g;
                    if (b < minB) minB = b;
                    if (b > maxB) maxB = b;
                }

                int rangeR = maxR - minR;
                int rangeG = maxG - minG;
                int rangeB = maxB - minB;

                int axis = 0;
                int range = rangeR;
                if (rangeG > range) { range = rangeG; axis = 1; }
                if (rangeB > range) { range = rangeB; axis = 2; }

                if (range > widestRange)
                {
                    widestRange = range;
                    targetBox = boxIndex;
                    splitAxis = axis;
                }
            }

            if (targetBox < 0)
                break; // nothing left to split

            var (start, count) = _retroBoxes[targetBox];
            int axisSelector = splitAxis;
            int[] distinct = _retroDistinct;

            Array.Sort(_retroOrder, start, count, Comparer<int>.Create((left, right) =>
                ChannelOf(distinct[left], axisSelector) - ChannelOf(distinct[right], axisSelector)));

            // Split at the population-weighted median.
            long totalWeight = 0;
            for (int k = start; k < start + count; k++)
                totalWeight += _retroCounts[_retroOrder[k]];

            long accumulatedWeight = 0;
            int splitAt = start + 1;
            for (int k = start; k < start + count; k++)
            {
                accumulatedWeight += _retroCounts[_retroOrder[k]];
                if (accumulatedWeight * 2 >= totalWeight)
                {
                    splitAt = k + 1;
                    break;
                }
            }

            if (splitAt <= start) splitAt = start + 1;
            if (splitAt >= start + count) splitAt = start + count - 1;

            _retroBoxes[targetBox] = (start, splitAt - start);
            _retroBoxes.Add((splitAt, start + count - splitAt));
        }
    }

    // Step 4 — one representative colour (population-weighted average) per box,
    // plus the distinct-colour → palette-index map used when remapping pixels.
    private void BuildPalette(int quantizationStep, bool snapToGrid)
    {
        for (int boxIndex = 0; boxIndex < _retroBoxes.Count; boxIndex++)
        {
            var (start, count) = _retroBoxes[boxIndex];

            long sumR = 0, sumG = 0, sumB = 0, sumWeight = 0;
            for (int k = start; k < start + count; k++)
            {
                int distinctIndex = _retroOrder[k];
                var (r, g, b) = UnpackColor(_retroDistinct[distinctIndex]);
                int weight = _retroCounts[distinctIndex];

                sumR += (long)r * weight;
                sumG += (long)g * weight;
                sumB += (long)b * weight;
                sumWeight += weight;

                _retroDistinctToPalette[distinctIndex] = boxIndex;
            }

            int averageR = sumWeight > 0 ? (int)(sumR / sumWeight) : 0;
            int averageG = sumWeight > 0 ? (int)(sumG / sumWeight) : 0;
            int averageB = sumWeight > 0 ? (int)(sumB / sumWeight) : 0;

            if (snapToGrid)
            {
                averageR = SnapToStep(averageR, quantizationStep);
                averageG = SnapToStep(averageG, quantizationStep);
                averageB = SnapToStep(averageB, quantizationStep);
            }

            _retroPalette[boxIndex] = (averageR << 16) | (averageG << 8) | averageB;
        }
    }

    // Step 5 — map every pixel to its palette colour via binary search over the
    // sorted distinct colours (every pixel colour is guaranteed present).
    private void RemapToPalette(byte[] pixels, int pixelCount, int distinctCount)
    {
        for (int p = 0, i = 0; p < pixelCount; p++, i += BPP)
        {
            int key = (pixels[i + 2] << 16) | (pixels[i + 1] << 8) | pixels[i + 0];
            int distinctIndex = Array.BinarySearch(_retroDistinct, 0, distinctCount, key);
            if (distinctIndex < 0)
                continue;

            int paletteColor = _retroPalette[_retroDistinctToPalette[distinctIndex]];
            pixels[i + 0] = (byte)(paletteColor & 0xFF);
            pixels[i + 1] = (byte)((paletteColor >> 8) & 0xFF);
            pixels[i + 2] = (byte)((paletteColor >> 16) & 0xFF);
        }
    }

    private static (int R, int G, int B) UnpackColor(int packed)
        => ((packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);

    private static int ChannelOf(int packed, int axis)
        => axis == 0 ? (packed >> 16) & 0xFF
         : axis == 1 ? (packed >> 8) & 0xFF
         : packed & 0xFF;

    private static int SnapToStep(int value, int quantizationStep)
    {
        if (quantizationStep <= 1)
            return value;

        int snapped = (int)MathF.Round((float)value / quantizationStep) * quantizationStep;
        if (snapped > 255) snapped = 255;
        return snapped;
    }
}
