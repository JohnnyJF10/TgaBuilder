using System;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Modifications
{
    // =========================================================================
    // ModificationsHelper — pure raw-array pipeline for single-texture edits
    // All adjustments operate on BGRA32 byte arrays.
    // The pipeline is:
    //   input → exposure → brightness/contrast → highlights/shadows/whites/blacks
    //         → hue/saturation/vibrance → temperature/tint → output
    // =========================================================================

    public class ModificationsHelper : IModificationsHelper
    {
        private const int BPP = 4; // BGRA32

        // =====================================================================
        // Dimensions
        // =====================================================================

        public int Width { get; set; }
        public int Height { get; set; }

        // =====================================================================
        // Basic adjustments  (range conventions match common image editors)
        // Exposure:    -5 .. +5  (stops)
        // Brightness:  -1 .. +1
        // Contrast:    -1 .. +1
        // Highlights:  -1 .. +1
        // Shadows:     -1 .. +1
        // Whites:      -1 .. +1
        // Blacks:      -1 .. +1
        // =====================================================================

        public float Exposure { get; set; } = 0f;
        public float Brightness { get; set; } = 0f;
        public float Contrast { get; set; } = 0f;
        public float Highlights { get; set; } = 0f;
        public float Shadows { get; set; } = 0f;
        public float Whites { get; set; } = 0f;
        public float Blacks { get; set; } = 0f;

        // =====================================================================
        // Color adjustments
        // Saturation:   -1 .. +1
        // Vibrance:     -1 .. +1
        // Hue:          -180 .. +180  (degrees)
        // Temperature:  -1 .. +1  (negative = cooler / blue, positive = warmer / orange)
        // Tint:         -1 .. +1  (negative = green, positive = magenta)
        // =====================================================================

        public float Saturation { get; set; } = 0f;
        public float Vibrance { get; set; } = 0f;
        public float Hue { get; set; } = 0f;
        public float Temperature { get; set; } = 0f;
        public float Tint { get; set; } = 0f;

        public Color ColorOverlay { get; set; } = new Color(0, 0, 0, 0);
        public float ColorOverlayAmount { get; set; } = 0f;
        public ColorOverlayMixMode ColorOverlayMixMode { get; set; } = ColorOverlayMixMode.Linear;
        public float ColorOverlaySoftLightStrength { get; set; } = 1f;
        public float ColorOverlayLumaPreservation { get; set; } = 1f;
        public float ColorOverlayChromaBoost { get; set; } = 1f;

        // =====================================================================
        // Pipeline entry point
        // =====================================================================

        public byte[] Apply(byte[] inputPixels)
        {
            int count = inputPixels.Length;
            byte[] output = new byte[count];
            Array.Copy(inputPixels, output, count);

            ApplyExposure(output);
            ApplyBrightnessContrast(output);
            ApplyHighlightsShadowsWhitesBlacks(output);
            ApplyHueSaturationVibrance(output);
            ApplyTemperatureTint(output);
            ApplyColorOverlay(output);

            return output;
        }

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

        // =====================================================================
        // Clamping helpers
        // =====================================================================

        private static byte Clamp(float v)
            => (byte)Math.Clamp((int)v, 0, 255);

        private static byte ClampF(float v)
            => (byte)Math.Clamp((int)Math.Round(v), 0, 255);

        private static byte Clamp01(float v)
            => (byte)Math.Clamp((int)(v * 255f + 0.5f), 0, 255);
    }
}
