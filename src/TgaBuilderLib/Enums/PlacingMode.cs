namespace TgaBuilderLib.Enums
{
    [Flags]
    public enum PlacingMode
    {
        Default = 0,
        OverlayTransparent = 1,
        ResizeToPicker = 2,
        PlaceContinuously = 4,
        PlaceAndSwap = 8,
    }
}
