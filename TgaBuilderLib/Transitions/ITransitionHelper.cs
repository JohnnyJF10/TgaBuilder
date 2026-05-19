using System.Collections.Generic;
using TgaBuilderLib.Abstraction;
using static TgaBuilderLib.Transitions.TransitionHelper;

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
        float Widening { get; set; }

        // Bricks / Segmented Transition Parameters
        BricksPipelineRequirements CurrentBricksPipelineRequirements { get; set; }
        bool InvertGrayscale { get; set; }
        int MarkerCount { get; set; }
        bool ReversePivot { get; set; }
        FilterType SelectedFilter { get; set; }
        SegmentationMethod SegmentationMethod { get; set; }
        int FelzenszwalbMinSize { get; set; }
        float FelzenszwalbScale { get; set; }
        int SlicSegmentCount { get; set; }
        float SlicCompactness { get; set; }
        int QuickshiftMaxDist { get; set; }
        float QuickshiftRatio { get; set; }
        Color EdgeColor { get; set; }
        float BilateralSigma { get; set; }
        float GaussianSigma { get; set; }
        int EdgeWidth { get; set; }
        EdgeBlendMode BlendMode { get; set; }
        bool SliceCornerTiles { get; set; }
        bool ProtectEdges { get; set; }
        float Shift { get; set; }

        // Methods
        byte[] MixSmooth(byte[] pixels1, byte[] pixels2);
        byte[] MixBricks(byte[] tilePixels, byte[] bgPixels);
        byte[] GetLabelMap();
        void CleanUp();
    }
}
