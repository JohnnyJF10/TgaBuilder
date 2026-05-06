using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Transitions
{
    public partial class TransitionHelper : ITransitionHelper
    {
        private const int TRANSITIONS_BPP = 4; // Assuming RGBA format
        public byte[] LastAnalysisMap { get; private set; } = Array.Empty<byte>();
        public int LastAnalysisWidth { get; private set; }
        public int LastAnalysisHeight { get; private set; }

        public int[] Labels { get; private set; } = Array.Empty<int>();

        public List<TileSegment> TileSegmentList { get; set; } = new List<TileSegment>();

        public bool[] Selection { get; set; } = Array.Empty<bool>();

        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public TransitionMode Mode { get; set; }

        public float Hardness { get; set; } = 0.5f;
        public float Pivot { get; set; } = 0.5f;
        public float Offset { get; set; } = 0f;


        public bool ReversePivot { get; set; } = false;
        public bool SliceCornerTiles { get; set; } = false;
        public int MarkerRadius { get; set; } = 3;
        public SegmentationMethod SegmentationMethod { get; set; } = SegmentationMethod.Watershed;
        public FilterType SelectedFilter { get; set; } = FilterType.BoxBlur;

        public Color EdgeColor { get; set; } = new Color(255, 255, 255, 128);

        public void CleanUp()
        {
            LastAnalysisMap = Array.Empty<byte>();
            LastAnalysisWidth = 0;
            LastAnalysisHeight = 0;
            Labels = Array.Empty<int>();
            TileSegmentList = new List<TileSegment>();
            Selection = Array.Empty<bool>();
            Width = 0;
            Height = 0;
            Stride = 0;
            Mode = TransitionMode.Top;
            Hardness = 0.5f;
            Pivot = 0.5f;
            Offset = 0f;
            MarkerRadius = 3; 
            ReversePivot = false;
            SliceCornerTiles = false;
            SegmentationMethod = SegmentationMethod.Watershed;
            SelectedFilter = FilterType.BoxBlur;
            EdgeColor = new Color(0, 0, 0, 128);
        }
    }
}
