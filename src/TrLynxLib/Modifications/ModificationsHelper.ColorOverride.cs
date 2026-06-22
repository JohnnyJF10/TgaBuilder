namespace TrLynxLib.Modifications;

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
    // The secondary texture is resized to the input size when it is loaded, so
    // it shares the input dimensions and the chroma fields are sampled 1:1.
    // All scratch buffers are provisioned by EnsureBuffers.
    // =====================================================================

    private void ApplyColorOverride(byte[] pixels)
    {
        if (!ColorOverrideEnabled)
            return;

        // Nothing to transfer until a secondary texture has been provided.
        if (!ColorOverrideHasSecondary)
            return;

        float amount = Math.Clamp(ColorOverrideAmount, 0f, 1f);
        if (amount <= 0.00001f)
            return;

        float decolorize = Math.Clamp(ColorOverrideDecolorize, 0f, 1f);
        float transfer = Math.Clamp(ColorOverrideTransfer, 0f, 1f);
        float chromaRestore = Math.Clamp(ColorOverrideChromaRestore, 0f, 2f);
        int radius = Math.Clamp(ColorOverrideSmoothing, 0, COLOR_OVERRIDE_SMOOTHING_MAX);

        int width = Width;
        int height = Height;
        int pixelCount = width * height;

        // Guard against any dimension / buffer mismatch before indexing.
        if (pixels.Length < pixelCount * BPP
            || PixelsSecondary.Length < pixelCount * BPP
            || _coSecA.Length < pixelCount)
            return;

        BuildSecondaryChroma(pixelCount);

        if (radius > 0)
            BlurSecondaryChroma(width, height, radius);

        for (int p = 0, i = 0; p < pixelCount; p++, i += BPP)
        {
            float b = pixels[i + 0] / 255f;
            float g = pixels[i + 1] / 255f;
            float r = pixels[i + 2] / 255f;

            var (lIn, aIn, bIn) = RgbToOklab((r, g, b));

            float aSec = _coSecA[p];
            float bSec = _coSecB[p];

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
            var (nr, ng, nb) = OklabToRgb((lIn, aMix, bMix));

            // 4. Master amount blend with the original pixel.
            nr = r + (nr - r) * amount;
            ng = g + (ng - g) * amount;
            nb = b + (nb - b) * amount;

            pixels[i + 0] = Clamp01(nb);
            pixels[i + 1] = Clamp01(ng);
            pixels[i + 2] = Clamp01(nr);
        }
    }

    // Converts the secondary texture into two OKLab chroma channels (a, b) at
    // the input resolution. Lightness is discarded — only colour transfers.
    private void BuildSecondaryChroma(int pixelCount)
    {
        for (int p = 0, i = 0; p < pixelCount; p++, i += BPP)
        {
            float b = PixelsSecondary[i + 0] / 255f;
            float g = PixelsSecondary[i + 1] / 255f;
            float r = PixelsSecondary[i + 2] / 255f;

            var (_, a, secondaryB) = RgbToOklab((r, g, b));
            _coSecA[p] = a;
            _coSecB[p] = secondaryB;
        }
    }

    private void BlurSecondaryChroma(int width, int height, int radius)
    {
        BoxBlurSeparable(_coSecA, _coScratch, width, height, radius);
        BoxBlurSeparable(_coSecB, _coScratch, width, height, radius);
    }

    // Separable box blur on a single float channel using prefix sums.
    // Edges use a shrinking (clamped) window so corners stay stable. O(width*height).
    private void BoxBlurSeparable(float[] data, float[] scratch, int width, int height, int radius)
    {
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
