namespace TgaBuilderLib.Enums
{
    [Flags]
    public enum FileTypes
    {
        None = 0,
        TGA = 1,
        BMP = 2,
        PNG = 4,
        JPG = 8,
        JPEG = 16,
        PSD = 32,
        KRA = 64,
        DDS = 128,
        PHD = 256,
        TR2 = 512,
        TR4 = 1024,
        TRC = 2048,
        TEN = 4096,
    }
}
