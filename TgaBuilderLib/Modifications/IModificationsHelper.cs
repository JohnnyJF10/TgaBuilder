using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Modifications
{
    public interface IModificationsHelper
    {
        // =====================================================================
        // Dimensions
        // =====================================================================

        int Width { get; set; }
        int Height { get; set; }

        // =====================================================================
        // Buffers
        // =====================================================================

        byte[] PixelsInput { get; set; }

        byte[] PixelsOutput { get; set; }

        byte[] PixelsSecondary { get; set; }

        int SecondaryWidth { get; set; }

        int SecondaryHeight { get; set; }

        // =====================================================================
        // Basic adjustments
        // =====================================================================

        float Exposure { get; set; }
        float Brightness { get; set; }
        float Contrast { get; set; }
        float Highlights { get; set; }
        float Shadows { get; set; }
        float Whites { get; set; }
        float Blacks { get; set; }

        // =====================================================================
        // Color adjustments
        // =====================================================================

        float Saturation { get; set; }
        float Vibrance { get; set; }
        float Hue { get; set; }
        float Temperature { get; set; }
        float Tint { get; set; }

        Color ColorOverlay { get; set; }
        float ColorOverlayAmount { get; set; }
        ColorOverlayMixMode ColorOverlayMixMode { get; set; }
        float ColorOverlaySoftLightStrength { get; set; }
        float ColorOverlayLumaPreservation { get; set; }
        float ColorOverlayChromaBoost { get; set; }

        bool ColorOverrideEnabled { get; set; }
        float ColorOverrideAmount { get; set; }
        float ColorOverrideDecolorize { get; set; }
        float ColorOverrideTransfer { get; set; }
        int ColorOverrideSmoothing { get; set; }
        float ColorOverrideChromaRestore { get; set; }

        // =====================================================================
        // Methods
        // =====================================================================

        void Apply();

        void CleanUp();

        Task QueueRecalc(Action? Configure = null);

        event EventHandler? RecalculationCompleted;
    }
}
