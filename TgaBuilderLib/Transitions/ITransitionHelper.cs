using System.Collections.Generic;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Transitions
{
    public interface ITransitionHelper
    {
        // Source Image Dimensions
        int Width { get; set; }
        int Height { get; set; }

        // Shared Transition Parameters
        TransitionMode Mode { get; set; }
        float Pivot { get; set; }

        // Smooth Transition Parameters
        float Hardness { get; set; }
        float Offset { get; set; }

        // Bricks / Segmented Transition Parameters
        BricksPipelineRequirements CurrentBricksPipelineRequirements { get; set; }
        bool InvertGrayscale { get; set; }
        int MarkerRadius { get; set; }
        bool ReversePivot { get; set; }
        FilterType SelectedFilter { get; set; }
        SegmentationMethod SegmentationMethod { get; set; }
        Color EdgeColor { get; set; }
        int EdgeWidth { get; set; }
        bool SliceCornerTiles { get; set; }

        // Methods
        byte[] MixSmooth(byte[] pixels1, byte[] pixels2);
        byte[] MixBricks(byte[] tilePixels, byte[] bgPixels);
        byte[] GetLabelMap();
        void CleanUp();
    }
}