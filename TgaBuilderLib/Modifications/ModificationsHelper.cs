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

    public partial class ModificationsHelper : IModificationsHelper
    {
        private const int BPP = 4; // BGRA32

        private const float EXPOSURE_INIT = 0f;
        private const float BRIGHTNESS_INIT = 0f;
        private const float CONTRAST_INIT = 0f;
        private const float HIGHLIGHTS_INIT = 0f;
        private const float SHADOWS_INIT = 0f;
        private const float WHITES_INIT = 0f;
        private const float BLACKS_INIT = 0f;

        private const float SATURATION_INIT = 0f;
        private const float VIBRANCE_INIT = 0f;
        private const float HUE_INIT = 0f;
        private const float TEMPERATURE_INIT = 0f;
        private const float TINT_INIT = 0f;

        private readonly Color COLOR_OVERLAY_INIT = new Color(0, 0, 0, 0);

        private const float COLOR_OVERLAY_AMOUNT_INIT = 0f;
        private const ColorOverlayMixMode COLOR_OVERLAY_MIX_MODE_INIT = ColorOverlayMixMode.Linear;

        private const float COLOR_OVERLAY_SOFT_LIGHT_STRENGTH_INIT = 1f;
        private const float COLOR_OVERLAY_LUMA_PRESERVATION_INIT = 1f;
        private const float COLOR_OVERLAY_CHROMA_BOOST_INIT = 1f;

        // =====================================================================
        // Dimensions
        // =====================================================================

        public int Width { get; set; } = 64;
        public int Height { get; set; } = 64;

        // =====================================================================
        // Buffers
        // =====================================================================

        public byte[] PixelsInput {get; set;} = new byte[64 * 64 * BPP];
        public byte[] PixelsOutput { get; set;} = new byte[64 * 64 * BPP];


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

        public float Exposure { get; set; } = EXPOSURE_INIT;
        public float Brightness { get; set; } = BRIGHTNESS_INIT;
        public float Contrast { get; set; } = CONTRAST_INIT;
        public float Highlights { get; set; } = HIGHLIGHTS_INIT;
        public float Shadows { get; set; } = SHADOWS_INIT;
        public float Whites { get; set; } = WHITES_INIT;
        public float Blacks { get; set; } = BLACKS_INIT;

        // =====================================================================
        // Color adjustments
        // Saturation:   -1 .. +1
        // Vibrance:     -1 .. +1
        // Hue:          -180 .. +180  (degrees)
        // Temperature:  -1 .. +1  (negative = cooler / blue, positive = warmer / orange)
        // Tint:         -1 .. +1  (negative = green, positive = magenta)
        // =====================================================================

        public float Saturation { get; set; } = SATURATION_INIT;
        public float Vibrance { get; set; } = VIBRANCE_INIT;
        public float Hue { get; set; } = HUE_INIT;
        public float Temperature { get; set; } = TEMPERATURE_INIT;
        public float Tint { get; set; } = TINT_INIT;

        public Color ColorOverlay { get; set; } = new Color(0, 0, 0, 0);
        public float ColorOverlayAmount { get; set; } = COLOR_OVERLAY_AMOUNT_INIT;
        public ColorOverlayMixMode ColorOverlayMixMode { get; set; } = COLOR_OVERLAY_MIX_MODE_INIT;
        public float ColorOverlaySoftLightStrength { get; set; } = COLOR_OVERLAY_SOFT_LIGHT_STRENGTH_INIT;
        public float ColorOverlayLumaPreservation { get; set; } = COLOR_OVERLAY_LUMA_PRESERVATION_INIT;
        public float ColorOverlayChromaBoost { get; set; } = COLOR_OVERLAY_CHROMA_BOOST_INIT;
        public event EventHandler? RecalculationCompleted;

        // =====================================================================
        // Pipeline entry point
        // =====================================================================

        public void Apply()
        {
            int count = PixelsInput.Length;

            if (PixelsOutput.Length != count)
                throw new InvalidOperationException("Input and output buffers must be the same size.");

            Array.Copy(PixelsInput, PixelsOutput, count);

            ApplyExposure(PixelsOutput);
            ApplyBrightnessContrast(PixelsOutput);
            ApplyHighlightsShadowsWhitesBlacks(PixelsOutput);
            ApplyHueSaturationVibrance(PixelsOutput);
            ApplyTemperatureTint(PixelsOutput);
            ApplyColorOverlay(PixelsOutput);
        }

        public void CleanUp()
        {
            Exposure = EXPOSURE_INIT;
            Brightness = BRIGHTNESS_INIT;
            Contrast = CONTRAST_INIT;
            Highlights = HIGHLIGHTS_INIT;
            Shadows = SHADOWS_INIT;
            Whites = WHITES_INIT;
            Blacks = BLACKS_INIT;

            Saturation = SATURATION_INIT;
            Vibrance = VIBRANCE_INIT;
            Hue = HUE_INIT;
            Temperature = TEMPERATURE_INIT;
            Tint = TINT_INIT;
            
            ColorOverlay = new Color(0, 0, 0, 0);
            ColorOverlayAmount = COLOR_OVERLAY_AMOUNT_INIT;
            ColorOverlayMixMode = COLOR_OVERLAY_MIX_MODE_INIT;
            ColorOverlaySoftLightStrength = COLOR_OVERLAY_SOFT_LIGHT_STRENGTH_INIT;
            ColorOverlayLumaPreservation = COLOR_OVERLAY_LUMA_PRESERVATION_INIT;
            ColorOverlayChromaBoost = COLOR_OVERLAY_CHROMA_BOOST_INIT;
        }
    }
}
