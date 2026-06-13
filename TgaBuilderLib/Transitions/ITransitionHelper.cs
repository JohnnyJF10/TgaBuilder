using System.Collections.Generic;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.Transitions
{
    public interface ITransitionHelper
    {
        bool IsActive { get; }

        // Source Image Dimensions
        int Width { get; }
        int Height { get; }

        byte[] Pixels1 { get; set; }
        byte[] Pixels2 { get; set; }
        
        byte[] PixelsResult { get; set; }

        TransitionType TypeOfTransition { get; set; }

        // Shared Transition Parameters
        TransitionDirection Direction { get; set; }
        float Pivot { get; set; }

        // Smooth Transition Parameters
        float Hardness { get; set; }
        float Widening { get; set; }

        // Bricks / Segmented Transition Parameters
        BricksPipelineRequirements CurrentBricksPipelineRequirements { get; set; }
        bool InvertGrayscale { get; set; }
        int MarkerCount { get; set; }
        int MarkerRadius { get; set; }
        float GridFitAngle { get; set; }
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
        int ShadowSize { get; set; }
        int ShadowHardness { get; set; }
        EdgeBlendMode BlendMode { get; set; }
        bool SliceCornerTiles { get; set; }
        bool ProtectEdges { get; set; }
        float Shift { get; set; }
        Color ShadowColor { get; set; }

        float UnderfillingPivot { get; set; }
        bool ReverseUnderfilling { get; set; }
        int UnderfillingThreshold { get; set; }

        // Methods
        void EnsureBuffers(int width, int height);
        void Mix();
        byte[] GetLabelMap();
        int GetLabelAtPixel(int x, int y);
        byte[] GetTileIndicator(int tileIndex);
        bool SetExplicitTileVisibility(int tileIndex, bool shouldDraw);

        void ResetAllExplicitTileVisibility();
        void CleanUp();

        Task QueueRecalc(Action? Configure = null);

        event EventHandler? RecalculationCompleted;
    }
}
