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

        private const bool COLOR_OVERRIDE_ENABLED_INIT = false;
        private const float COLOR_OVERRIDE_AMOUNT_INIT = 1f;
        private const float COLOR_OVERRIDE_DECOLORIZE_INIT = 1f;
        private const float COLOR_OVERRIDE_TRANSFER_INIT = 1f;
        private const int COLOR_OVERRIDE_SMOOTHING_INIT = 0;
        private const int COLOR_OVERRIDE_SMOOTHING_MAX = 32;
        private const float COLOR_OVERRIDE_CHROMA_RESTORE_INIT = 1f;

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

        // Secondary input texture used by the Color Override stage. Stored at
        // its own resolution; sampled with normalised coordinates at apply time.
        public byte[] PixelsSecondary { get; set; } = Array.Empty<byte>();
        public int SecondaryWidth { get; set; } = 0;
        public int SecondaryHeight { get; set; } = 0;


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

        // =====================================================================
        // Color Override adjustments  (texture-to-texture colour transfer)
        // Enabled:        on/off toggle for the whole stage
        // Amount:         0 .. 1   master blend with the original
        // Decolorize:     0 .. 1   removes the original texture's own colour
        // Transfer:       0 .. 1   applies the secondary input's colour
        // Smoothing:      0 .. 32  box-blur radius of the secondary colour field
        // ChromaRestore:  0 .. 2   scales / restores the resulting colour amount
        // =====================================================================

        public bool ColorOverrideEnabled { get; set; } = COLOR_OVERRIDE_ENABLED_INIT;
        public float ColorOverrideAmount { get; set; } = COLOR_OVERRIDE_AMOUNT_INIT;
        public float ColorOverrideDecolorize { get; set; } = COLOR_OVERRIDE_DECOLORIZE_INIT;
        public float ColorOverrideTransfer { get; set; } = COLOR_OVERRIDE_TRANSFER_INIT;
        public int ColorOverrideSmoothing { get; set; } = COLOR_OVERRIDE_SMOOTHING_INIT;
        public float ColorOverrideChromaRestore { get; set; } = COLOR_OVERRIDE_CHROMA_RESTORE_INIT;

        public event EventHandler? RecalculationCompleted;

        // =====================================================================
        // Pipeline entry point
        // =====================================================================

        public void Apply()
        {
            int count = PixelsInput.Length;

            PixelsOutput = new byte[count];

            Array.Copy(PixelsInput, PixelsOutput, count);

            ApplyExposure(PixelsOutput);
            ApplyBrightnessContrast(PixelsOutput);
            ApplyHighlightsShadowsWhitesBlacks(PixelsOutput);
            ApplyHueSaturationVibrance(PixelsOutput);
            ApplyTemperatureTint(PixelsOutput);
            ApplyColorOverlay(PixelsOutput);
            ApplyColorOverride(PixelsOutput);
        }

        public void CleanUp()
        {
            Width = 64;
            Height = 64;

            PixelsInput = new byte[64 * 64 * BPP];
            PixelsOutput = new byte[64 * 64 * BPP];

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

            PixelsSecondary = Array.Empty<byte>();
            SecondaryWidth = 0;
            SecondaryHeight = 0;

            ColorOverrideEnabled = COLOR_OVERRIDE_ENABLED_INIT;
            ColorOverrideAmount = COLOR_OVERRIDE_AMOUNT_INIT;
            ColorOverrideDecolorize = COLOR_OVERRIDE_DECOLORIZE_INIT;
            ColorOverrideTransfer = COLOR_OVERRIDE_TRANSFER_INIT;
            ColorOverrideSmoothing = COLOR_OVERRIDE_SMOOTHING_INIT;
            ColorOverrideChromaRestore = COLOR_OVERRIDE_CHROMA_RESTORE_INIT;

            _coSecA = null;
            _coSecB = null;
            _coScratch = null;
            _coPrefix = null;
        }
    }
}
