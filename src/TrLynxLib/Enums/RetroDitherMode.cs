namespace TrLynxLib.Enums
{
    // =====================================================================
    // Ordered dithering pattern used by the Texture Retrofier.
    // Checkerboard reproduces the iconic alternating-pixel retro look;
    // the Bayer matrices give finer ordered dithering.
    // =====================================================================
    public enum RetroDitherMode
    {
        None = 0,
        Checkerboard = 1,
        Bayer4x4 = 2,
        Bayer8x8 = 3
    }
}
