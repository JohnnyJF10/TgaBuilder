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
            RgbToHsl(r, g, b, out float h, out float s, out float l);
            // Hue shift
            h = (h + hueDelta + 1f) % 1f;
            // Saturation (simple additive)
            s = Math.Clamp(s + Saturation, 0f, 1f);
            // Vibrance: boosts low-saturation colors more than high-saturation
            float vibranceEffect = Vibrance * (1f - s);
            s = Math.Clamp(s + vibranceEffect, 0f, 1f);
            HslToRgb(h, s, l, out float nr, out float ng, out float nb);
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
    private static void RgbToHsl(float r, float g, float b,
        out float h, out float s, out float l)
    {
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;
        l = (max + min) * 0.5f;
        if (delta < 0.00001f)
        {
            h = 0f;
            s = 0f;
            return;
        }
        s = delta / (1f - Math.Abs(2f * l - 1f));
        if (max == r)
            h = ((g - b) / delta % 6f) / 6f;
        else if (max == g)
            h = ((b - r) / delta + 2f) / 6f;
        else
            h = ((r - g) / delta + 4f) / 6f;
        if (h < 0f) h += 1f;
    }
    private static void HslToRgb(float h, float s, float l,
        out float r, out float g, out float b)
    {
        float c = (1f - Math.Abs(2f * l - 1f)) * s;
        float x = c * (1f - Math.Abs(h * 6f % 2f - 1f));
        float m = l - c * 0.5f;
        float r1, g1, b1;
        int seg = (int)(h * 6f) % 6;
        switch (seg)
        {
            case 0: r1 = c; g1 = x; b1 = 0; break;
            case 1: r1 = x; g1 = c; b1 = 0; break;
            case 2: r1 = 0; g1 = c; b1 = x; break;
            case 3: r1 = 0; g1 = x; b1 = c; break;
            case 4: r1 = x; g1 = 0; b1 = c; break;
            default: r1 = c; g1 = 0; b1 = x; break;
        }
        r = r1 + m;
        g = g1 + m;
        b = b1 + m;
    }
}