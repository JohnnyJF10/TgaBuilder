using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Transitions
{
    public partial class TransitionHelper : ITransitionHelper
    {
        public TransitionHelper(Color? AccentColor = null) 
        { 
            _systemAccentColor = AccentColor ?? new Color(128, 128, 128, 128);
            Pixels1 = new byte[64 * 64 * TRANSITIONS_BPP];
            Pixels2 = new byte[64 * 64 * TRANSITIONS_BPP];
            PixelsResult = new byte[64 * 64 * TRANSITIONS_BPP];
        }
        private readonly Color _systemAccentColor;
        private const int TRANSITIONS_BPP = 4; // Always BGRA32


        public byte[] Pixels1 { get; set; }
        public byte[] Pixels2 { get; set; }

        public byte[] PixelsResult { get; set; }


        private int[] _labels = new int[64 * 64];
        private List<TileSegment> _tileSegmentList = new();
        private bool[] _selection = new bool[64 * 64];

        // Reusable scratch buffers provisioned by EnsureBuffers when the view opens or the
        // input picture sizes change, reused across recalcs, and released in CleanUp.
        private float[] _scratchFiltered = new float[64 * 64];
        private float[] _scratchGray = new float[64 * 64];
        private byte[] _scratchFilteredColor = new byte[64 * 64 * TRANSITIONS_BPP];
        private byte[] _scratchShadowedBg = new byte[64 * 64 * TRANSITIONS_BPP];
        private byte[] _scratchLabelMap = new byte[64 * 64 * TRANSITIONS_BPP];

        private int _provisionedWidth;
        private int _provisionedHeight;

        // Because _labels/_selection are now pre-provisioned (length is always Width*Height),
        // their array length can no longer signal "never computed". These flags do.
        private bool _labelsBuilt;
        private bool _selectionBuilt;

        public int Width { get; set; } = 64;
        public int Height { get; set; } = 64;

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

        public event EventHandler? RecalculationCompleted;

        public void Mix()
        {
            if (TypeOfTransition == TransitionType.Smooth)
                MixSmooth(Pixels1, Pixels2, PixelsResult);
            else
                MixBricks(Pixels1, Pixels2, PixelsResult);
        }

        // Provisions the reusable buffer set for the given input picture size. Called when the
        // transitions view opens and whenever the input picture dimensions change. Cheap no-op
        // when the size is unchanged, so callers may invoke it freely.
        public void EnsureBuffers(int width, int height)
        {
            int n = width * height;

            if (width == _provisionedWidth
                && height == _provisionedHeight
                && _scratchFiltered.Length == n)
                return;

            int n4 = n * TRANSITIONS_BPP;

            Pixels1 = new byte[n4];
            Pixels2 = new byte[n4];
            PixelsResult = new byte[n4];

            _labels = new int[n];
            _selection = new bool[n];

            _scratchFiltered = new float[n];
            _scratchGray = new float[n];
            _scratchFilteredColor = new byte[n4];
            _scratchShadowedBg = new byte[n4];
            _scratchLabelMap = new byte[n4];

            _provisionedWidth = width;
            _provisionedHeight = height;

            _labelsBuilt = false;
            _selectionBuilt = false;
        }

        public void CleanUp()
        {
            _labels = new int[64 * 64];
            _tileSegmentList = new List<TileSegment>();
            _selection = new bool[64 * 64];

            // Release the reusable scratch buffers so their memory can be reclaimed while the
            // view is closed.
            _scratchFiltered = new float[64 * 64];
            _scratchGray = new float[64 * 64];
            _scratchFilteredColor = new byte[64 * 64 * TRANSITIONS_BPP];
            _scratchShadowedBg = new byte[64 * 64 * TRANSITIONS_BPP];
            _scratchLabelMap = new byte[64 * 64 * TRANSITIONS_BPP];

            _provisionedWidth = 0;
            _provisionedHeight = 0;
            _labelsBuilt = false;
            _selectionBuilt = false;

            Width = 64;
            Height = 64;

            Pixels1 = new byte[64 * 64 * TRANSITIONS_BPP];
            Pixels2 = new byte[64 * 64 * TRANSITIONS_BPP];
            PixelsResult = new byte[64 * 64 * TRANSITIONS_BPP];

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
            UnderfillingPivot = 0.5f;
            ReverseUnderfilling = false;
            UnderfillingThreshold = 0;
        }
    }
}
