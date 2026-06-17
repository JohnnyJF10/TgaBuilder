namespace TgaBuilderLib.Modifications;

public partial class ModificationsHelper
{
    // =====================================================================
    // Stage 7 — Color Override (texture-to-texture colour transfer)
    //
    // Re-colours the input texture using the colours of a secondary input
    // texture while keeping the input's own luminance (its structure /
    // detail). The whole operation happens in the perceptual OKLab colour
    // space, so only the chroma channels (a, b) are exchanged — the
    // lightness (L) of the original pixel is always preserved.
    //
    // Process chain (per pixel):
    //   1. Decolorize  — fade the original chroma toward neutral grey.
    //   2. Transfer    — blend in the (optionally filtered) secondary chroma.
    //   3. ChromaRestore — scale the resulting chroma to restore / boost the
    //                      amount of colour in the coloured regions.
    //   4. Amount      — master blend between the original pixel and the
    //                    re-coloured pixel.
    //
    // Filtering (a separable box blur on the secondary chroma field) lets the
    // user choose between transferring specific local colours (radius 0) and
    // transferring an overall averaged tone (large radius).
    // =====================================================================

    // Cached secondary chroma fields (secondary resolution). Reused across
    // recalculations and only re-allocated when the secondary size changes.
    private float[]? _coSecA;
    private float[]? _coSecB;
    private float[]? _coScratch;
    private float[]? _coPrefix;

    private void ApplyColorOverride(byte[] pixels)
    {
        if (!ColorOverrideEnabled)
            return;

        // Nothing to transfer until a secondary texture has been provided.
        if (SecondaryWidth <= 0 || SecondaryHeight <= 0)
            return;

        if (PixelsSecondary.Length < SecondaryWidth * SecondaryHeight * BPP)
            return;

        float amount = Math.Clamp(ColorOverrideAmount, 0f, 1f);
        if (amount <= 0.00001f)
            return;

        float decolorize = Math.Clamp(ColorOverrideDecolorize, 0f, 1f);
        float transfer = Math.Clamp(ColorOverrideTransfer, 0f, 1f);
        float chromaRestore = Math.Clamp(ColorOverrideChromaRestore, 0f, 2f);
        int radius = Math.Clamp(ColorOverrideSmoothing, 0, COLOR_OVERRIDE_SMOOTHING_MAX);

        BuildSecondaryChroma();

        if (radius > 0)
            BlurSecondaryChroma(radius);

        int width = Width;
        int height = Height;
        int secondaryWidth = SecondaryWidth;
        int secondaryHeight = SecondaryHeight;

        // Guard against any dimension / buffer mismatch before 2D indexing.
        if (pixels.Length < width * height * BPP)
            return;

        float[] secondaryA = _coSecA!;
        float[] secondaryB = _coSecB!;

        for (int y = 0; y < height; y++)
        {
            // Map the output row onto a secondary row (normalised sampling so
            // any size mismatch is handled gracefully; identity when equal).
            int sy = secondaryHeight == height ? y : (int)((long)y * secondaryHeight / height);
            if (sy >= secondaryHeight) sy = secondaryHeight - 1;

            int rowStart = y * width * BPP;
            int secondaryRowStart = sy * secondaryWidth;

            for (int x = 0; x < width; x++)
            {
                int i = rowStart + x * BPP;

                float b = pixels[i + 0] / 255f;
                float g = pixels[i + 1] / 255f;
                float r = pixels[i + 2] / 255f;

                RgbToOklab(r, g, b, out float lIn, out float aIn, out float bIn);

                int sx = secondaryWidth == width ? x : (int)((long)x * secondaryWidth / width);
                if (sx >= secondaryWidth) sx = secondaryWidth - 1;
                int si = secondaryRowStart + sx;

                float aSec = secondaryA[si];
                float bSec = secondaryB[si];

                // 1. Remove colour from the original texture.
                float aBase = aIn * (1f - decolorize);
                float bBase = bIn * (1f - decolorize);

                // 2. Apply colour from the secondary input.
                float aMix = aBase + (aSec - aBase) * transfer;
                float bMix = bBase + (bSec - bBase) * transfer;

                // 3. Restore / boost the amount of colour.
                aMix *= chromaRestore;
                bMix *= chromaRestore;

                // Re-combine with the untouched original lightness.
                OklabToRgb(lIn, aMix, bMix, out float nr, out float ng, out float nb);

                // 4. Master amount blend with the original pixel.
                nr = r + (nr - r) * amount;
                ng = g + (ng - g) * amount;
                nb = b + (nb - b) * amount;

                pixels[i + 0] = Clamp01(nb);
                pixels[i + 1] = Clamp01(ng);
                pixels[i + 2] = Clamp01(nr);
            }
        }
    }

    // Converts the secondary texture into two OKLab chroma channels (a, b)
    // at its own resolution. Lightness is discarded — only colour transfers.
    private void BuildSecondaryChroma()
    {
        int pixelCount = SecondaryWidth * SecondaryHeight;

        if (_coSecA is null || _coSecA.Length != pixelCount)
            _coSecA = new float[pixelCount];
        if (_coSecB is null || _coSecB.Length != pixelCount)
            _coSecB = new float[pixelCount];

        for (int p = 0; p < pixelCount; p++)
        {
            int i = p * BPP;
            float b = PixelsSecondary[i + 0] / 255f;
            float g = PixelsSecondary[i + 1] / 255f;
            float r = PixelsSecondary[i + 2] / 255f;

            RgbToOklab(r, g, b, out _, out float a, out float ob);
            _coSecA[p] = a;
            _coSecB[p] = ob;
        }
    }

    private void BlurSecondaryChroma(int radius)
    {
        int pixelCount = _coSecA!.Length;
        if (_coScratch is null || _coScratch.Length != pixelCount)
            _coScratch = new float[pixelCount];

        BoxBlurSeparable(_coSecA!, _coScratch, SecondaryWidth, SecondaryHeight, radius);
        BoxBlurSeparable(_coSecB!, _coScratch, SecondaryWidth, SecondaryHeight, radius);
    }

    // Separable box blur on a single float channel using prefix sums.
    // Edges use a shrinking (clamped) window so corners stay stable. O(width*height).
    private void BoxBlurSeparable(float[] data, float[] scratch, int width, int height, int radius)
    {
        int maxDimension = Math.Max(width, height);
        if (_coPrefix is null || _coPrefix.Length < maxDimension + 1)
            _coPrefix = new float[maxDimension + 1];

        float[] prefix = _coPrefix;

        // Horizontal pass: data -> scratch
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width;
            prefix[0] = 0f;
            for (int x = 0; x < width; x++)
                prefix[x + 1] = prefix[x] + data[rowStart + x];

            for (int x = 0; x < width; x++)
            {
                int x0 = x - radius;
                if (x0 < 0) x0 = 0;
                int x1 = x + radius;
                if (x1 > width - 1) x1 = width - 1;

                scratch[rowStart + x] = (prefix[x1 + 1] - prefix[x0]) / (x1 - x0 + 1);
            }
        }

        // Vertical pass: scratch -> data
        for (int x = 0; x < width; x++)
        {
            prefix[0] = 0f;
            for (int y = 0; y < height; y++)
                prefix[y + 1] = prefix[y] + scratch[y * width + x];

            for (int y = 0; y < height; y++)
            {
                int y0 = y - radius;
                if (y0 < 0) y0 = 0;
                int y1 = y + radius;
                if (y1 > height - 1) y1 = height - 1;

                data[y * width + x] = (prefix[y1 + 1] - prefix[y0]) / (y1 - y0 + 1);
            }
        }
    }
}
