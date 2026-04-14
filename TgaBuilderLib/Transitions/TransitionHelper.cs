using System;
using System.Collections.Generic;
using System.Text;

namespace TgaBuilderLib.Transitions
{
    public partial class TransitionHelper : ITransitionHelper
    {
        public byte[] LastAnalysisMap { get; private set; } = Array.Empty<byte>();
        public int LastAnalysisWidth { get; private set; }
        public int LastAnalysisHeight { get; private set; }

        public int[] Labels { get; private set; } = Array.Empty<int>();

        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public int Bpp { get; set; }
        public TransitionMode Mode { get; set; }

        public List<TileSegment> TileData { get; set; } = new List<TileSegment>();
        public float Hardness { get; set; } = 0.5f;
        public float Pivot { get; set; } = 0.5f;


        public bool ReversePivot { get; set; } = false;
        public int MarkerRadius { get; set; } = 3;
        public int ExpectedRegionCount { get; set; } = -1;

        public void CleanUp()
        {
            LastAnalysisMap = Array.Empty<byte>();
            LastAnalysisWidth = 0;
            LastAnalysisHeight = 0;
            Labels = Array.Empty<int>();
            TileData.Clear();
            Hardness = 0.5f;
            Pivot = 0.5f;
            MarkerRadius = 3; 
            ExpectedRegionCount = 0;
            ReversePivot = false;

        }
    }
}
