using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Transitions
{
    public partial class TransitionHelper : ITransitionHelper
    {
        private const int TRANSITIONS_BPP = 4; // Always BGRA32

        private int[] _labels = Array.Empty<int>();
        private List<TileSegment> _tileSegmentList = new();
        private bool[] _selection = Array.Empty<bool>();

        public int Width { get; set; }
        public int Height { get; set; }

        public TransitionMode Mode { get; set; }
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
        public int MarkerRadius { get; set; } = 3;
        public SegmentationMethod SegmentationMethod { get; set; } = SegmentationMethod.Watershed;
        public int FelzenszwalbMinSize { get; set; } = 50;
        public float FelzenszwalbScale { get; set; } = 100f;
        public int SlicSegmentCount { get; set; } = 250;
        public float SlicCompactness { get; set; } = 10f;
        public int QuickshiftMaxDist { get; set; } = 10;
        public float QuickshiftRatio { get; set; } = 1f;
        public FilterType SelectedFilter { get; set; } = FilterType.BoxBlur;
        public Color EdgeColor { get; set; } = new Color(255, 255, 255, 128);
        public EdgeBlendMode BlendMode { get; set; } = EdgeBlendMode.Multiply;
        public int EdgeWidth { get; set; } = 1;

        public void CleanUp()
        {
            _labels = Array.Empty<int>();
            _tileSegmentList = new List<TileSegment>();
            _selection = Array.Empty<bool>();

            Width = 0;
            Height = 0;

            Mode = TransitionMode.Top;
            Pivot = 0.5f;

            Hardness = 0.5f;
            Widening = 0f;

            MarkerRadius = 3; 
            ReversePivot = false;
            SliceCornerTiles = false;
            ProtectEdges = true;
            SegmentationMethod = SegmentationMethod.Watershed;
            FelzenszwalbMinSize = 50;
            FelzenszwalbScale = 100f;
            SlicSegmentCount = 250;
            SlicCompactness = 10f;
            QuickshiftMaxDist = 10;
            QuickshiftRatio = 1f;
            SelectedFilter = FilterType.BoxBlur;
            EdgeColor = new Color(0, 0, 0, 128);
            BlendMode = EdgeBlendMode.Multiply;
            EdgeWidth = 1;
        }
    }
}
