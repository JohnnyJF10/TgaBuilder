using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Transitions
{
    public interface ITransitionHelper
    {
        float Hardness { get; set; }
        int Height { get; set; }
        int[] Labels { get; }
        (float X, float Y)[] Centroids { get; set; }
        int LastAnalysisHeight { get; }
        byte[] LastAnalysisMap { get; }
        int LastAnalysisWidth { get; }
        int MarkerRadius { get; set; }
        TransitionMode Mode { get; set; }
        float Offset { get; set; }
        float Pivot { get; set; }
        bool ReversePivot { get; set; }
        FilterType SelectedFilter { get; set; }
        SegmentationMethod SegmentationMethod { get; set; }
        Color EdgeColor { get; set; }
        bool SliceCornerTiles { get; set; }
        int Stride { get; set; }
        int Width { get; set; }

        void AnalyzeTiles(byte[] pixels);
        byte[] MixPixels(byte[] pixels1, byte[] pixels2);
        byte[] MixSmartTilesPixels(byte[] tilePixels, byte[] bgPixels);

        void CleanUp();
    }
}