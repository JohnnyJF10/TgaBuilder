namespace TgaBuilderLib.Abstraction
{
    [Flags]
    public enum PlacingMode
    {
        Default = 0,
        OverlayTransparent = 1,
        PlaceAndSwap = 2,
        ResizeToPicker = 4
    }
}
