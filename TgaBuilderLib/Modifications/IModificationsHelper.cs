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

        // =====================================================================
        // Pipeline
        // =====================================================================

        byte[] Apply(byte[] inputPixels);
    }
}
