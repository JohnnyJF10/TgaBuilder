using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Modifications;

public partial class ModificationsHelper
{
    // =====================================================================
    // Stage 6 — Color Overlay
    // Amount: 0 .. 1
    // Mix modes:
    //   - Linear: linear interpolation in display space
    //   - SoftLight: filmic soft-light blend
    //   - OklabChroma: perceptual chroma transfer in OKLab color space
    // =====================================================================
    private void ApplyColorOverlay(byte[] pixels)
    {
        float amount = Math.Clamp(ColorOverlayAmount, 0f, 1f);
        if (amount <= 0.00001f)
            return;

        (float R, float G, float B) overlay =
            (ColorOverlay.R / 255f, ColorOverlay.G / 255f, ColorOverlay.B / 255f);

        var mixMode = ColorOverlayMixMode;

        int len = pixels.Length;
        for (int i = 0; i < len; i += BPP)
        {
            (float R, float G, float B) baseColor =
                (pixels[i + 2] / 255f, pixels[i + 1] / 255f, pixels[i + 0] / 255f);

            var (nr, ng, nb) = mixMode switch
            {
                ColorOverlayMixMode.SoftLight => BlendSoftLight(baseColor, overlay, amount),
                ColorOverlayMixMode.OklabChroma => BlendOklabChroma(baseColor, overlay, amount),
                _ => BlendLinear(baseColor, overlay, amount)
            };

            pixels[i + 0] = Clamp01(nb);
            pixels[i + 1] = Clamp01(ng);
            pixels[i + 2] = Clamp01(nr);
        }
    }

    private static (float R, float G, float B) BlendLinear(
        (float R, float G, float B) baseColor,
        (float R, float G, float B) overlay,
        float amount)
        => (
            Lerp(baseColor.R, overlay.R, amount),
            Lerp(baseColor.G, overlay.G, amount),
            Lerp(baseColor.B, overlay.B, amount));

    private (float R, float G, float B) BlendSoftLight(
        (float R, float G, float B) baseColor,
        (float R, float G, float B) overlay,
        float amount)
    {
        float strength = Math.Clamp(ColorOverlaySoftLightStrength, 0f, 1f);
        float sr = SoftLight(baseColor.R, overlay.R);
        float sg = SoftLight(baseColor.G, overlay.G);
        float sb = SoftLight(baseColor.B, overlay.B);
        sr = Lerp(baseColor.R, sr, strength);
        sg = Lerp(baseColor.G, sg, strength);
        sb = Lerp(baseColor.B, sb, strength);
        return (
            Lerp(baseColor.R, sr, amount),
            Lerp(baseColor.G, sg, amount),
            Lerp(baseColor.B, sb, amount));
    }

    private (float R, float G, float B) BlendOklabChroma(
        (float R, float G, float B) baseColor,
        (float R, float G, float B) overlay,
        float amount)
    {
        float lumaPreservation = Math.Clamp(ColorOverlayLumaPreservation, 0f, 1f);
        float chromaBoost = Math.Clamp(ColorOverlayChromaBoost, 0f, 2f);
        var (l1, a1, b1) = RgbToOklab(baseColor);
        var (l2, a2, b2) = RgbToOklab(overlay);
        float outL = Lerp(l1, l2, amount * (1f - lumaPreservation));
        float outA = Lerp(a1, a2 * chromaBoost, amount);
        float outB = Lerp(b1, b2 * chromaBoost, amount);
        var (r, g, b) = OklabToRgb((outL, outA, outB));
        return (
            Math.Clamp(r, 0f, 1f),
            Math.Clamp(g, 0f, 1f),
            Math.Clamp(b, 0f, 1f));
    }

    private static float SoftLight(float baseColor, float blendColor)
    {
        if (blendColor <= 0.5f)
            return baseColor - (1f - 2f * blendColor) * baseColor * (1f - baseColor);
        float d = baseColor <= 0.25f
            ? ((16f * baseColor - 12f) * baseColor + 4f) * baseColor
            : MathF.Sqrt(baseColor);
        return baseColor + (2f * blendColor - 1f) * (d - baseColor);
    }
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    // =====================================================================
    // OKLab helpers (perceptual color space)
    // =====================================================================
    private static (float L, float A, float B) RgbToOklab((float R, float G, float B) c)
    {
        float lr = SrgbToLinear(c.R);
        float lg = SrgbToLinear(c.G);
        float lb = SrgbToLinear(c.B);
        float x = 0.4122214708f * lr + 0.5363325363f * lg + 0.0514459929f * lb;
        float y = 0.2119034982f * lr + 0.6806995451f * lg + 0.1073969566f * lb;
        float z = 0.0883024619f * lr + 0.2817188376f * lg + 0.6299787005f * lb;
        float lx = Cbrt(x);
        float ly = Cbrt(y);
        float lz = Cbrt(z);
        float l = 0.2104542553f * lx + 0.7936177850f * ly - 0.0040720468f * lz;
        float a = 1.9779984951f * lx - 2.4285922050f * ly + 0.4505937099f * lz;
        float bAxis = 0.0259040371f * lx + 0.7827717662f * ly - 0.8086757660f * lz;
        return (l, a, bAxis);
    }
    private static (float R, float G, float B) OklabToRgb((float L, float A, float B) c)
    {
        float lx = c.L + 0.3963377774f * c.A + 0.2158037573f * c.B;
        float ly = c.L - 0.1055613458f * c.A - 0.0638541728f * c.B;
        float lz = c.L - 0.0894841775f * c.A - 1.2914855480f * c.B;
        float x = lx * lx * lx;
        float y = ly * ly * ly;
        float z = lz * lz * lz;
        float lr = +4.0767416621f * x - 3.3077115913f * y + 0.2309699292f * z;
        float lg = -1.2684380046f * x + 2.6097574011f * y - 0.3413193965f * z;
        float lb = -0.0041960863f * x - 0.7034186147f * y + 1.7076147010f * z;
        float r = LinearToSrgb(lr);
        float g = LinearToSrgb(lg);
        float b = LinearToSrgb(lb);
        return (r, g, b);
    }
    private static float Cbrt(float v)
        => v < 0f
            ? -MathF.Pow(-v, 1f / 3f)
            : MathF.Pow(v, 1f / 3f);
    private static float SrgbToLinear(float c)
        => c <= 0.04045f
            ? c / 12.92f
            : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    private static float LinearToSrgb(float c)
    {
        c = Math.Clamp(c, 0f, 1f);
        return c <= 0.0031308f
            ? c * 12.92f
            : 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;
    }
}
