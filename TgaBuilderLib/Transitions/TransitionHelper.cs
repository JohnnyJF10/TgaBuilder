using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace TgaBuilderLib.Transitions
{
    public partial class TransitionHelper : ITransitionHelper
    {
        private const int TRANSITIONS_BPP = 4; // Assuming RGBA format
        public byte[] LastAnalysisMap { get; private set; } = Array.Empty<byte>();
        public int LastAnalysisWidth { get; private set; }
        public int LastAnalysisHeight { get; private set; }

        public int[] Labels { get; private set; } = Array.Empty<int>();

        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public TransitionMode Mode { get; set; }

        public List<TileSegment> TileData { get; set; } = new List<TileSegment>();
        public float Hardness { get; set; } = 0.5f;
        public float Pivot { get; set; } = 0.5f;


        public bool ReversePivot { get; set; } = false;
        public bool SliceCornerTiles { get; set; } = false;
        public int MarkerRadius { get; set; } = 3;
        public SegmentationMethod SegmentationMethod { get; set; } = SegmentationMethod.Watershed;
        public FilterType SelectedFilter { get; set; } = FilterType.BoxBlur;

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
            ReversePivot = false;
            SliceCornerTiles = false;
            SegmentationMethod = SegmentationMethod.Watershed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Computes directional distances to texture domains for the selected transition mode.
        private (float distToT1, float distToT2) ComputeTopologicy(TransitionMode mode, float nx, float ny)
            => mode switch
            {
                TransitionMode.Top => (
                    Math.Min(nx, Math.Min(1.0f - nx, 1.0f - ny)),
                    ny
                ),

                TransitionMode.Bottom => (
                    Math.Min(nx, Math.Min(1.0f - nx, ny)),
                    1.0f - ny
                ),

                TransitionMode.Left => (
                    Math.Min(ny, Math.Min(1.0f - ny, 1.0f - nx)),
                    nx
                ),

                TransitionMode.Right => (
                    Math.Min(ny, Math.Min(1.0f - ny, nx)),
                    1.0f - nx
                ),

                TransitionMode.DiagonalTopLeft => (
                    Math.Min(1.0f - nx, 1.0f - ny),
                    Math.Min(nx, ny)
                ),

                TransitionMode.DiagonalTopRight => (
                    Math.Min(nx, 1.0f - ny),
                    Math.Min(1.0f - nx, ny)
                ),

                _ => (0f, 0f)
            };
    }
}
