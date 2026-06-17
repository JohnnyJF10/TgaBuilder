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
    // Palette limitation via median cut (one step per helper)
    // ---------------------------------------------------------------------
    private void ApplyRetroPalette(
        byte[] pixels, int width, int height, int maxColors, int quantizationStep, bool snapToGrid)
    {
        int pixelDataLength = width * height * BPP;

        var colors = BuildColorHistogram(pixels, pixelDataLength);
        if (colors.Length <= maxColors)
            return; // already within the limit

        var (order, boxes) = MedianCut(colors, maxColors);
        var palette = BuildPalette(colors, order, boxes, quantizationStep, snapToGrid);
        RemapToPalette(pixels, pixelDataLength, palette);
    }

    // Step 1 — distinct RGB colours with their pixel counts.
    private static (byte R, byte G, byte B, int Weight)[] BuildColorHistogram(
        byte[] pixels, int pixelDataLength)
    {
        var histogram = new Dictionary<int, int>();
        for (int i = 0; i < pixelDataLength; i += BPP)
        {
            int key = (pixels[i + 2] << 16) | (pixels[i + 1] << 8) | pixels[i + 0];
            histogram.TryGetValue(key, out int count);
            histogram[key] = count + 1;
        }

        var colors = new (byte R, byte G, byte B, int Weight)[histogram.Count];
        int index = 0;
        foreach (var entry in histogram)
        {
            colors[index++] = (
                (byte)((entry.Key >> 16) & 0xFF),
                (byte)((entry.Key >> 8) & 0xFF),
                (byte)(entry.Key & 0xFF),
                entry.Value);
        }

        return colors;
    }

    // Step 2 — median cut: boxes are ranges into a reorderable index array.
    private static (int[] Order, List<(int Start, int Count)> Boxes) MedianCut(
        (byte R, byte G, byte B, int Weight)[] colors, int maxColors)
    {
        int colorCount = colors.Length;

        int[] order = new int[colorCount];
        for (int i = 0; i < colorCount; i++)
            order[i] = i;

        var boxes = new List<(int Start, int Count)> { (0, colorCount) };

        while (boxes.Count < maxColors)
        {
            int targetBox = -1;
            int widestRange = 0;
            int splitAxis = 0;

            for (int boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
            {
                var (boxStart, boxCount) = boxes[boxIndex];
                if (boxCount <= 1)
                    continue;

                byte minR = 255, maxR = 0, minG = 255, maxG = 0, minB = 255, maxB = 0;
                for (int k = boxStart; k < boxStart + boxCount; k++)
                {
                    var color = colors[order[k]];
                    if (color.R < minR) minR = color.R;
                    if (color.R > maxR) maxR = color.R;
                    if (color.G < minG) minG = color.G;
                    if (color.G > maxG) maxG = color.G;
                    if (color.B < minB) minB = color.B;
                    if (color.B > maxB) maxB = color.B;
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

            var (start, count) = boxes[targetBox];
            int axisSelector = splitAxis;

            Array.Sort(order, start, count, Comparer<int>.Create((left, right) =>
            {
                int leftValue = axisSelector == 0 ? colors[left].R : axisSelector == 1 ? colors[left].G : colors[left].B;
                int rightValue = axisSelector == 0 ? colors[right].R : axisSelector == 1 ? colors[right].G : colors[right].B;
                return leftValue - rightValue;
            }));

            // Split at the population-weighted median.
            long totalWeight = 0;
            for (int k = start; k < start + count; k++)
                totalWeight += colors[order[k]].Weight;

            long accumulatedWeight = 0;
            int splitAt = start + 1;
            for (int k = start; k < start + count; k++)
            {
                accumulatedWeight += colors[order[k]].Weight;
                if (accumulatedWeight * 2 >= totalWeight)
                {
                    splitAt = k + 1;
                    break;
                }
            }

            if (splitAt <= start) splitAt = start + 1;
            if (splitAt >= start + count) splitAt = start + count - 1;

            boxes[targetBox] = (start, splitAt - start);
            boxes.Add((splitAt, start + count - splitAt));
        }

        return (order, boxes);
    }

    // Step 3 — one representative colour (population-weighted average) per box.
    private static (byte R, byte G, byte B)[] BuildPalette(
        (byte R, byte G, byte B, int Weight)[] colors,
        int[] order,
        List<(int Start, int Count)> boxes,
        int quantizationStep,
        bool snapToGrid)
    {
        var palette = new (byte R, byte G, byte B)[boxes.Count];

        for (int boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            var (start, count) = boxes[boxIndex];

            long sumR = 0, sumG = 0, sumB = 0, sumWeight = 0;
            for (int k = start; k < start + count; k++)
            {
                var color = colors[order[k]];
                sumR += (long)color.R * color.Weight;
                sumG += (long)color.G * color.Weight;
                sumB += (long)color.B * color.Weight;
                sumWeight += color.Weight;
            }

            byte averageR = sumWeight > 0 ? (byte)(sumR / sumWeight) : (byte)0;
            byte averageG = sumWeight > 0 ? (byte)(sumG / sumWeight) : (byte)0;
            byte averageB = sumWeight > 0 ? (byte)(sumB / sumWeight) : (byte)0;

            if (snapToGrid)
            {
                averageR = SnapToStep(averageR, quantizationStep);
                averageG = SnapToStep(averageG, quantizationStep);
                averageB = SnapToStep(averageB, quantizationStep);
            }

            palette[boxIndex] = (averageR, averageG, averageB);
        }

        return palette;
    }

    // Step 4 — map every pixel to its nearest palette colour (cached per colour).
    private static void RemapToPalette(
        byte[] pixels, int pixelDataLength, (byte R, byte G, byte B)[] palette)
    {
        int paletteCount = palette.Length;
        var mapCache = new Dictionary<int, int>(paletteCount);

        for (int i = 0; i < pixelDataLength; i += BPP)
        {
            int key = (pixels[i + 2] << 16) | (pixels[i + 1] << 8) | pixels[i + 0];

            if (!mapCache.TryGetValue(key, out int paletteIndex))
            {
                int red = pixels[i + 2];
                int green = pixels[i + 1];
                int blue = pixels[i + 0];

                int best = 0;
                long bestDistance = long.MaxValue;
                for (int q = 0; q < paletteCount; q++)
                {
                    long deltaR = red - palette[q].R;
                    long deltaG = green - palette[q].G;
                    long deltaB = blue - palette[q].B;
                    long distance = deltaR * deltaR + deltaG * deltaG + deltaB * deltaB;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = q;
                    }
                }

                paletteIndex = best;
                mapCache[key] = paletteIndex;
            }

            var entry = palette[paletteIndex];
            pixels[i + 0] = entry.B;
            pixels[i + 1] = entry.G;
            pixels[i + 2] = entry.R;
        }
    }

    private static byte SnapToStep(byte value, int quantizationStep)
    {
        if (quantizationStep <= 1)
            return value;

        int snapped = (int)MathF.Round((float)value / quantizationStep) * quantizationStep;
        if (snapped > 255) snapped = 255;
        return (byte)snapped;
    }
}
