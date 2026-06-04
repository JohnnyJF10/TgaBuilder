using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TgaBuilderLib.Abstraction;
using Transitions;

namespace TgaBuilderLib.Transitions
{
    public partial class TransitionHelper : ITransitionHelper
    {
        public TransitionHelper(Color? AccentColor = null) 
        { 
            _systemAccentColor = AccentColor ?? new Color(128, 128, 128, 128);
            pixels1 = new byte[64 * 64 * TRANSITIONS_BPP];
            pixels2 = new byte[64 * 64 * TRANSITIONS_BPP];
        }
        private readonly Color _systemAccentColor;
        private const int TRANSITIONS_BPP = 4; // Always BGRA32


        public byte[] pixels1 { get; set; }
        public byte[] pixels2 { get; set; }


        private int[] _labels = Array.Empty<int>();
        private List<TileSegment> _tileSegmentList = new();
        private bool[] _selection = Array.Empty<bool>();

        public int Width { get; set; }
        public int Height { get; set; }

        public TransitionType TypeOfTransition { get; set; }

        public TransitionDirection Direction { get; set; }
        public float Pivot { get; set; } = 0.5f;

        public float Hardness { get; set; } = 0.5f;
        public float Widening { get; set; } = 0f;
        public float Shift { get; set; } = 0f;


        public BricksPipelineRequirements CurrentBricksPipelineRequirements { get; set; }
            = BricksPipelineRequirements.RequiresAnalysis;
        public bool ReversePivot { get; set; } = false;
        public bool InvertGrayscale { get; set; } = false;
        public bool SliceCornerTiles { get; set; } = false;
        public bool ProtectEdges { get; set; } = true;
        public int MarkerCount { get; set; } = 42;
        public int MarkerRadius { get; set; } = 5;
        public float GridFitAngle { get; set; } = 0f;
        public SegmentationMethod SegmentationMethod { get; set; } = SegmentationMethod.Felzenszwalb;
        public int FelzenszwalbMinSize { get; set; } = 50;
        public float FelzenszwalbScale { get; set; } = 100f;
        public int SlicSegmentCount { get; set; } = 250;
        public float SlicCompactness { get; set; } = 10f;
        public int QuickshiftMaxDist { get; set; } = 10;
        public float QuickshiftRatio { get; set; } = 1f;
        public FilterType SelectedFilter { get; set; } = FilterType.Gaussian;
        public float BilateralSigma { get; set; } = 30f;
        public float GaussianSigma { get; set; } = 1f;

        public float UnderfillingPivot { get; set; } = 0.5f;
        public bool ReverseUnderfilling { get; set; } = false;
        public int UnderfillingThreshold { get; set; } = 0;

        public Color EdgeColor { get; set; } = new Color(255, 255, 255, 128);
        public EdgeBlendMode BlendMode { get; set; } = EdgeBlendMode.Multiply;
        public int EdgeWidth { get; set; } = 1;
        public Color ShadowColor { get; set; } = new Color(42, 42, 42, 42);
        public int ShadowSize { get; set; } = 3;
        public int ShadowHardness { get; set; } = 50;

        public byte[] Mix()
        {
            if (TypeOfTransition == TransitionType.Smooth)
                return MixSmooth(pixels1, pixels2);
            else
                return MixBricks(pixels1, pixels2);
        }

        public void CleanUp()
        {
            _labels = Array.Empty<int>();
            _tileSegmentList = new List<TileSegment>();
            _selection = Array.Empty<bool>();

            Width = 0;
            Height = 0;

            pixels1 = new byte[64 * 64 * TRANSITIONS_BPP];
            pixels2 = new byte[64 * 64 * TRANSITIONS_BPP];

            Direction = TransitionDirection.Top;
            Pivot = 0.5f;

            Hardness = 0.5f;
            Widening = 0f;

            MarkerCount = 42; 
            MarkerRadius = 5;
            GridFitAngle = 0f;
            ReversePivot = false;
            SliceCornerTiles = false;
            ProtectEdges = true;
            SegmentationMethod = SegmentationMethod.Felzenszwalb;
            FelzenszwalbMinSize = 50;
            FelzenszwalbScale = 100f;
            SlicSegmentCount = 250;
            SlicCompactness = 10f;
            QuickshiftMaxDist = 10;
            QuickshiftRatio = 1f;
            SelectedFilter = FilterType.BoxBlur;
            BilateralSigma = 30f;
            GaussianSigma = 1f;
            EdgeColor = new Color(0, 0, 0, 128);
            BlendMode = EdgeBlendMode.Multiply;
            EdgeWidth = 1;
            ShadowColor = new Color(42, 42, 42, 42);
            ShadowSize = 3;
            ShadowHardness = 50;
        }
    }
}
