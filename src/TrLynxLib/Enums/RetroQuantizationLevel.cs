namespace TrLynxLib.Enums
{
    // =====================================================================
    // Per-channel colour quantization used by the Texture Retrofier.
    // TR1/TR2 (Sega Saturn) used a reduced colour space where RGB channel
    // values are commonly multiples of 8 (5-bit), sometimes 4 (6-bit).
    // =====================================================================
    public enum RetroQuantizationLevel
    {
        None = 0,      // full 8-bit precision (no change)
        SixBit = 1,    // multiples of 4
        FiveBit = 2,   // multiples of 8   (classic TR look)
        FourBit = 3    // multiples of 16
    }
}
