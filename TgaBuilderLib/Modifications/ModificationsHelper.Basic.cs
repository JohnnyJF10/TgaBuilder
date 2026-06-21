namespace TgaBuilderLib.Modifications;

public partial class ModificationsHelper
{
    // =====================================================================
    // Stage 1 — Exposure (multiplicative, in stops)
    // =====================================================================
    private void ApplyExposure(byte[] pixels)
    {
        if (Exposure == 0f) return;
        float factor = MathF.Pow(2f, Exposure);
        int len = pixels.Length;
        for (int i = 0; i < len; i += BPP)
        {
            pixels[i + 0] = Clamp(pixels[i + 0] * factor); // B
            pixels[i + 1] = Clamp(pixels[i + 1] * factor); // G
            pixels[i + 2] = Clamp(pixels[i + 2] * factor); // R
            // alpha unchanged
        }
    }
    // =====================================================================
    // Stage 2 — Brightness / Contrast
    // Brightness: linear offset (+/- 128)
    // Contrast:   S-curve around 128
    // =====================================================================
    private void ApplyBrightnessContrast(byte[] pixels)
    {
        if (Brightness == 0f && Contrast == 0f) return;
        float bOffset = Brightness * 128f;
        float cFactor = Contrast >= 0f
            ? 1f + Contrast * 3f
            : 1f + Contrast;     // compress toward 0.5 when negative
        int len = pixels.Length;
        for (int i = 0; i < len; i += BPP)
        {
            pixels[i + 0] = ClampF(ApplyBC(pixels[i + 0], bOffset, cFactor));
            pixels[i + 1] = ClampF(ApplyBC(pixels[i + 1], bOffset, cFactor));
            pixels[i + 2] = ClampF(ApplyBC(pixels[i + 2], bOffset, cFactor));
        }
    }
    private static float ApplyBC(byte channel, float bOffset, float cFactor)
    {
        float v = channel + bOffset;
        v = (v - 128f) * cFactor + 128f;
        return v;
    }
    // =====================================================================
    // Stage 3 — Highlights / Shadows / Whites / Blacks
    // Operates on luminance, applies corrections to bright/dark regions.
    // =====================================================================
    private void ApplyHighlightsShadowsWhitesBlacks(byte[] pixels)
    {
        if (Highlights == 0f && Shadows == 0f && Whites == 0f && Blacks == 0f) return;
        int len = pixels.Length;
        for (int i = 0; i < len; i += BPP)
        {
            float b = pixels[i + 0] / 255f;
            float g = pixels[i + 1] / 255f;
            float r = pixels[i + 2] / 255f;
            float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            // Highlights: weight peaks near 1
            float hWeight = lum * lum;
            // Shadows: weight peaks near 0
            float sWeight = (1f - lum) * (1f - lum);
            // Whites: pure top end
            float wWeight = lum >= 0.75f ? (lum - 0.75f) / 0.25f : 0f;
            // Blacks: pure bottom end
            float bkWeight = lum <= 0.25f ? (0.25f - lum) / 0.25f : 0f;
            float dr = Highlights * hWeight + Shadows * sWeight + Whites * wWeight - Blacks * bkWeight;
            float dg = dr;
            float db = dr;
            pixels[i + 0] = Clamp01(b + db);
            pixels[i + 1] = Clamp01(g + dg);
            pixels[i + 2] = Clamp01(r + dr);
        }
    }
}