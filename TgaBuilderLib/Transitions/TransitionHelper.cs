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
        public float Offset { get; set; } = 0f;


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
            Offset = 0f;
            MarkerRadius = 3; 
            ReversePivot = false;
            SliceCornerTiles = false;
            SegmentationMethod = SegmentationMethod.Watershed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Computes directional distances to texture domains for the selected transition mode.
        // Used as-is by brick transitions. Smooth transitions use ComputeTopologicyForSmooth.
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Smooth-transition topology with offset support. When offset > 0:
        // - Cardinal modes: the strip at the T2 edge is pure T2; the remaining band is
        //   remapped to [0,1] so the full topology gradient fills it with no hard cut.
        // - Diagonal modes: an L-shaped T2 zone (two strips at the T2-originating edges);
        //   the remaining square is remapped to [0,1]×[0,1].
        private (float distToT1, float distToT2) ComputeTopologicyForSmooth(
            TransitionMode mode, float nx, float ny, float offset)
        {
            if (offset <= 0f)
                return ComputeTopologicy(mode, nx, ny);

            float remaining = 1f - offset;
            if (remaining <= 0f)
                return (1f, 0f);

            switch (mode)
            {
                case TransitionMode.Top:
                    if (ny < offset) return (1f, 0f);
                    return ComputeTopologicy(mode, nx, (ny - offset) / remaining);

                case TransitionMode.Bottom:
                    if (ny > 1f - offset) return (1f, 0f);
                    return ComputeTopologicy(mode, nx, ny / remaining);

                case TransitionMode.Left:
                    if (nx < offset) return (1f, 0f);
                    return ComputeTopologicy(mode, (nx - offset) / remaining, ny);

                case TransitionMode.Right:
                    if (nx > 1f - offset) return (1f, 0f);
                    return ComputeTopologicy(mode, nx / remaining, ny);

                case TransitionMode.DiagonalTopLeft:
                    if (ny < offset || nx < offset) return (1f, 0f);
                    return ComputeTopologicy(mode, (nx - offset) / remaining, (ny - offset) / remaining);

                case TransitionMode.DiagonalTopRight:
                    if (ny < offset || nx > 1f - offset) return (1f, 0f);
                    return ComputeTopologicy(mode, nx / remaining, (ny - offset) / remaining);

                default:
                    return ComputeTopologicy(mode, nx, ny);
            }
        }
    }
}
