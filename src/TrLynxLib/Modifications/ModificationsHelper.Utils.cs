namespace TrLynxLib.Modifications;

public partial class ModificationsHelper
{
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