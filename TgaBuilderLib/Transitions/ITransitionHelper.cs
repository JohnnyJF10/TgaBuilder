using System.Collections.Generic;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Transitions
{
    public interface ITransitionHelper
    {
        float Hardness { get; set; }
        int Height { get; set; }
        int[] Labels { get; }
        List<TileSegment> TileSegmentList { get; set; }

        bool[] Selection { get; set; }

        //int LastAnalysisHeight { get; }
        //byte[] LastAnalysisMap { get; }
        //int LastAnalysisWidth { get; }
        int MarkerRadius { get; set; }
        TransitionMode Mode { get; set; }
        float Offset { get; set; }
        float Pivot { get; set; }

        BricksPipelineRequirements CurrentBricksPipelineRequirements { get; set; }
        bool ReversePivot { get; set; }
        FilterType SelectedFilter { get; set; }
        SegmentationMethod SegmentationMethod { get; set; }
        Color EdgeColor { get; set; }
        int EdgeWidth { get; set; }
        bool SliceCornerTiles { get; set; }
        int Width { get; set; }


        byte[] MixSmooth(byte[] pixels1, byte[] pixels2);
        byte[] MixBricks(byte[] tilePixels, byte[] bgPixels);

        byte[] GetLabelMap();

        void CleanUp();
    }
}