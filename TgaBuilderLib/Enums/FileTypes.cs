﻿namespace TgaBuilderLib.Enums
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
        DDS = 64,
        PHD = 128,
        TR2 = 256,
        TR4 = 512,
        TRC = 1024,
        TEN = 2048,
    }
}
