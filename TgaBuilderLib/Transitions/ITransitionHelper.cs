namespace TgaBuilderLib.Transitions
{
    public interface ITransitionHelper
    {
        float Hardness { get; set; }
        int Height { get; set; }
        int[] Labels { get; }
        int LastAnalysisHeight { get; }
        byte[] LastAnalysisMap { get; }
        int LastAnalysisWidth { get; }
        int MarkerRadius { get; set; }
        TransitionMode Mode { get; set; }
        float Pivot { get; set; }
        bool ReversePivot { get; set; }
        bool SliceCornerTiles { get; set; }
        int Stride { get; set; }
        List<TileSegment> TileData { get; set; }
        int Width { get; set; }

        void AnalyzeTilesWatershed(byte[] pixels);
        byte[] MixPixels(byte[] pixels1, byte[] pixels2);
        byte[] MixSmartTilesPixels(byte[] tilePixels, byte[] bgPixels);

        void CleanUp();
    }
}