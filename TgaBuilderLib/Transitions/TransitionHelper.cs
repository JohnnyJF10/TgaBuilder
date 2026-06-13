using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Transitions;
public partial class TransitionHelper : ITransitionHelper
{
    private const int TRANSITIONS_BPP = 4; // Always BGRA32
    private const int INIT_SIZE = 64;


    public TransitionHelper(Color? AccentColor = null)
    {
        _systemAccentColor = AccentColor ?? new Color(128, 128, 128, 128);
    }
    public bool IsActive => Width > 0 && Height > 0;
    private readonly Color _systemAccentColor;

    public byte[] Pixels1 { get; set; } = Array.Empty<byte>();
    public byte[] Pixels2 { get; set; } = Array.Empty<byte>();
    public byte[] PixelsResult { get; set; } = Array.Empty<byte>();
    private int[] _labels = Array.Empty<int>();
    private List<TileSegment> _tileSegmentList = new();
    private bool[] _selection = Array.Empty<bool>();
    // Reusable scratch buffers provisioned by EnsureBuffers when the view opens or the
    // input picture sizes change, reused across recalcs, and released in CleanUp.
    private float[] _scratchFiltered = Array.Empty<float>();
    private float[] _scratchGray = Array.Empty<float>();
    private byte[] _scratchFilteredColor = Array.Empty<byte>();
    private byte[] _scratchShadowedBg = Array.Empty<byte>();
    private byte[] _scratchLabelMap = Array.Empty<byte>();
    // Per-pixel Chebyshev distance to the nearest opposite-selection pixel, precomputed once
    // per selection change and reused by both shadow and edge drawing passes.
    private int[] _edgeDist = Array.Empty<int>();

    private bool _labelsBuilt;
    private bool _selectionBuilt;
    // _edgeDist depends only on the selection, so it is recomputed only when the selection
    // changes (set false after BuildSelection); drawing-only recalcs reuse it.
    private bool _edgeDistValid;
    public int Width { get; private set; } = -1;
    public int Height { get; private set; } = -1;
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
        if (!IsActive)
            return;

        if (TypeOfTransition == TransitionType.Smooth)
            MixSmooth(Pixels1, Pixels2, PixelsResult);
        else
            MixBricks(Pixels1, Pixels2, PixelsResult);
    }

    //public void SetUp()
    //{
    //    _labels = new int[INIT_SIZE * INIT_SIZE];
    //    _tileSegmentList = new List<TileSegment>();
    //    _selection = new bool[INIT_SIZE * INIT_SIZE];
    //    // Release the reusable scratch buffers so their memory can be reclaimed while the
    //    // view is closed.
    //    _scratchFiltered = new float[INIT_SIZE * INIT_SIZE];
    //    _scratchGray = new float[INIT_SIZE * INIT_SIZE];
    //    _scratchFilteredColor = new byte[INIT_SIZE * INIT_SIZE * TRANSITIONS_BPP];
    //    _scratchShadowedBg = new byte[INIT_SIZE * INIT_SIZE * TRANSITIONS_BPP];
    //    _scratchLabelMap = new byte[INIT_SIZE * INIT_SIZE * TRANSITIONS_BPP];
//
    //    Width = INIT_SIZE;
    //    Height = INIT_SIZE;
    //    Pixels1 = new byte[INIT_SIZE * INIT_SIZE * TRANSITIONS_BPP];
    //    Pixels2 = new byte[INIT_SIZE * INIT_SIZE * TRANSITIONS_BPP];
    //    PixelsResult = new byte[INIT_SIZE * INIT_SIZE * TRANSITIONS_BPP];
//
    //    IsActive = true;
    //}

    public void CleanUp()
    {
        _labels = Array.Empty<int>();
        _tileSegmentList = new List<TileSegment>();
        _selection = Array.Empty<bool>();

        _scratchFiltered = Array.Empty<float>();
        _scratchGray = Array.Empty<float>();
        _scratchFilteredColor = Array.Empty<byte>();
        _scratchShadowedBg = Array.Empty<byte>();
        _scratchLabelMap = Array.Empty<byte>();
        _edgeDist = Array.Empty<int>();

        _labelsBuilt = false;
        _selectionBuilt = false;
        _edgeDistValid = false;

        Width = -1;
        Height = -1;

        Pixels1 = Array.Empty<byte>();
        Pixels2 = Array.Empty<byte>();
        PixelsResult = Array.Empty<byte>();

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
