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
        float or = ColorOverlay.R / 255f;
        float og = ColorOverlay.G / 255f;
        float ob = ColorOverlay.B / 255f;
        int len = pixels.Length;
        for (int i = 0; i < len; i += BPP)
        {
            float b = pixels[i + 0] / 255f;
            float g = pixels[i + 1] / 255f;
            float r = pixels[i + 2] / 255f;
            float nr;
            float ng;
            float nb;
            switch (ColorOverlayMixMode)
            {
                case ColorOverlayMixMode.SoftLight:
                    BlendSoftLight(r, g, b, or, og, ob, amount, out nr, out ng, out nb);
                    break;
                case ColorOverlayMixMode.OklabChroma:
                    BlendOklabChroma(r, g, b, or, og, ob, amount, out nr, out ng, out nb);
                    break;
                default:
                    BlendLinear(r, g, b, or, og, ob, amount, out nr, out ng, out nb);
                    break;
            }
            pixels[i + 0] = Clamp01(nb);
            pixels[i + 1] = Clamp01(ng);
            pixels[i + 2] = Clamp01(nr);
        }
    }
    private static void BlendLinear(
        float r,
        float g,
        float b,
        float or,
        float og,
        float ob,
        float amount,
        out float nr,
        out float ng,
        out float nb)
    {
        nr = Lerp(r, or, amount);
        ng = Lerp(g, og, amount);
        nb = Lerp(b, ob, amount);
    }
    private void BlendSoftLight(
        float r,
        float g,
        float b,
        float or,
        float og,
        float ob,
        float amount,
        out float nr,
        out float ng,
        out float nb)
    {
        float strength = Math.Clamp(ColorOverlaySoftLightStrength, 0f, 1f);
        float sr = SoftLight(r, or);
        float sg = SoftLight(g, og);
        float sb = SoftLight(b, ob);
        sr = Lerp(r, sr, strength);
        sg = Lerp(g, sg, strength);
        sb = Lerp(b, sb, strength);
        nr = Lerp(r, sr, amount);
        ng = Lerp(g, sg, amount);
        nb = Lerp(b, sb, amount);
    }
    private void BlendOklabChroma(
        float r,
        float g,
        float b,
        float or,
        float og,
        float ob,
        float amount,
        out float nr,
        out float ng,
        out float nb)
    {
        float lumaPreservation = Math.Clamp(ColorOverlayLumaPreservation, 0f, 1f);
        float chromaBoost = Math.Clamp(ColorOverlayChromaBoost, 0f, 2f);
        RgbToOklab(r, g, b, out float l1, out float a1, out float b1);
        RgbToOklab(or, og, ob, out float l2, out float a2, out float b2);
        float outL = Lerp(l1, l2, amount * (1f - lumaPreservation));
        float outA = Lerp(a1, a2 * chromaBoost, amount);
        float outB = Lerp(b1, b2 * chromaBoost, amount);
        OklabToRgb(outL, outA, outB, out nr, out ng, out nb);
        nr = Math.Clamp(nr, 0f, 1f);
        ng = Math.Clamp(ng, 0f, 1f);
        nb = Math.Clamp(nb, 0f, 1f);
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
    private static void RgbToOklab(
        float r,
        float g,
        float b,
        out float l,
        out float a,
        out float ob)
    {
        float lr = SrgbToLinear(r);
        float lg = SrgbToLinear(g);
        float lb = SrgbToLinear(b);
        float x = 0.4122214708f * lr + 0.5363325363f * lg + 0.0514459929f * lb;
        float y = 0.2119034982f * lr + 0.6806995451f * lg + 0.1073969566f * lb;
        float z = 0.0883024619f * lr + 0.2817188376f * lg + 0.6299787005f * lb;
        float lx = Cbrt(x);
        float ly = Cbrt(y);
        float lz = Cbrt(z);
        l = 0.2104542553f * lx + 0.7936177850f * ly - 0.0040720468f * lz;
        a = 1.9779984951f * lx - 2.4285922050f * ly + 0.4505937099f * lz;
        ob = 0.0259040371f * lx + 0.7827717662f * ly - 0.8086757660f * lz;
    }
    private static void OklabToRgb(
        float l,
        float a,
        float ob,
        out float r,
        out float g,
        out float b)
    {
        float lx = l + 0.3963377774f * a + 0.2158037573f * ob;
        float ly = l - 0.1055613458f * a - 0.0638541728f * ob;
        float lz = l - 0.0894841775f * a - 1.2914855480f * ob;
        float x = lx * lx * lx;
        float y = ly * ly * ly;
        float z = lz * lz * lz;
        float lr = +4.0767416621f * x - 3.3077115913f * y + 0.2309699292f * z;
        float lg = -1.2684380046f * x + 2.6097574011f * y - 0.3413193965f * z;
        float lb = -0.0041960863f * x - 0.7034186147f * y + 1.7076147010f * z;
        r = LinearToSrgb(lr);
        g = LinearToSrgb(lg);
        b = LinearToSrgb(lb);
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