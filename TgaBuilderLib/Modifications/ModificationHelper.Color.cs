namespace TgaBuilderLib.Modifications;

public partial class ModificationsHelper
{
    // =====================================================================
    // Stage 4 — Hue / Saturation / Vibrance
    // Converts RGB→HSL, applies adjustments, converts back.
    // Vibrance is a smart-saturation that protects already-saturated colors.
    // =====================================================================
    private void ApplyHueSaturationVibrance(byte[] pixels)
    {
        if (Hue == 0f && Saturation == 0f && Vibrance == 0f) return;
        float hueDelta = Hue / 360f;
        int len = pixels.Length;
        for (int i = 0; i < len; i += BPP)
        {
            float b = pixels[i + 0] / 255f;
            float g = pixels[i + 1] / 255f;
            float r = pixels[i + 2] / 255f;
            var (h, s, l) = RgbToHsl((r, g, b));
            // Hue shift
            h = (h + hueDelta + 1f) % 1f;
            // Saturation (simple additive)
            s = Math.Clamp(s + Saturation, 0f, 1f);
            // Vibrance: boosts low-saturation colors more than high-saturation
            float vibranceEffect = Vibrance * (1f - s);
            s = Math.Clamp(s + vibranceEffect, 0f, 1f);
            var (nr, ng, nb) = HslToRgb((h, s, l));
            pixels[i + 0] = Clamp01(nb);
            pixels[i + 1] = Clamp01(ng);
            pixels[i + 2] = Clamp01(nr);
        }
    }
    // =====================================================================
    // Stage 5 — Temperature / Tint
    // Temperature shifts toward warm (orange/red) or cool (blue).
    // Tint shifts toward magenta or green.
    // =====================================================================
    private void ApplyTemperatureTint(byte[] pixels)
    {
        if (Temperature == 0f && Tint == 0f) return;
        int len = pixels.Length;
        for (int i = 0; i < len; i += BPP)
        {
            float b = pixels[i + 0] / 255f;
            float g = pixels[i + 1] / 255f;
            float r = pixels[i + 2] / 255f;
            // Warm → add red/orange, reduce blue
            r = Math.Clamp(r + Temperature * 0.3f, 0f, 1f);
            g = Math.Clamp(g + Temperature * 0.1f, 0f, 1f);
            b = Math.Clamp(b - Temperature * 0.3f, 0f, 1f);
            // Tint → add magenta (red+blue) or green
            r = Math.Clamp(r + Tint * 0.2f, 0f, 1f);
            g = Math.Clamp(g - Tint * 0.2f, 0f, 1f);
            b = Math.Clamp(b + Tint * 0.1f, 0f, 1f);
            pixels[i + 0] = Clamp01(b);
            pixels[i + 1] = Clamp01(g);
            pixels[i + 2] = Clamp01(r);
        }
    }

    // =====================================================================
    // HSL helpers
    // =====================================================================
    private static (float H, float S, float L) RgbToHsl((float R, float G, float B) c)
    {
        float r = c.R, g = c.G, b = c.B;
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;
        float l = (max + min) * 0.5f;
        if (delta < 0.00001f)
            return (0f, 0f, l);
        float s = delta / (1f - Math.Abs(2f * l - 1f));
        float h;
        if (max == r)
            h = ((g - b) / delta % 6f) / 6f;
        else if (max == g)
            h = ((b - r) / delta + 2f) / 6f;
        else
            h = ((r - g) / delta + 4f) / 6f;
        if (h < 0f) h += 1f;
        return (h, s, l);
    }
    private static (float R, float G, float B) HslToRgb((float H, float S, float L) c)
    {
        float h = c.H, s = c.S, l = c.L;
        float chroma = (1f - Math.Abs(2f * l - 1f)) * s;
        float x = chroma * (1f - Math.Abs(h * 6f % 2f - 1f));
        float m = l - chroma * 0.5f;
        float r1, g1, b1;
        int seg = (int)(h * 6f) % 6;
        switch (seg)
        {
            case 0: r1 = chroma; g1 = x; b1 = 0; break;
            case 1: r1 = x; g1 = chroma; b1 = 0; break;
            case 2: r1 = 0; g1 = chroma; b1 = x; break;
            case 3: r1 = 0; g1 = x; b1 = chroma; break;
            case 4: r1 = x; g1 = 0; b1 = chroma; break;
            default: r1 = chroma; g1 = 0; b1 = x; break;
        }
        return (r1 + m, g1 + m, b1 + m);
    }
}
