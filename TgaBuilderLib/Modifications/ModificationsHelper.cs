using System;
using TgaBuilderLib.Abstraction;

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
        public Color ColorTarget { get; set; } = new Color(0, 0, 0, 0);

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
