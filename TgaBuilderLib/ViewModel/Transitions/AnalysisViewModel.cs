using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick Analysis expander section.
/// Controls segmentation method, filter, and analysis parameters.
/// </summary>
public class TransitionAnalysisViewModel : ThrottledViewModelBase
{
    public TransitionAnalysisViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        TransitionPresentersViewModel presenters)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        Presenters = presenters;
    }

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    public TransitionPresentersViewModel Presenters { get; }

    // =====================================================================
    // Fields
    // =====================================================================

    private bool _invertGrayscale;
    private int _markerCount = 42;
    private int _markerRadius = 5;
    private float _gridFitAngle = 0f;
    private FilterType _selectedFilter = FilterType.Gaussian;
    private SegmentationMethod _selectedSegmentationMethod = SegmentationMethod.Felzenszwalb;
    private int _felzenszwalbMinSize = 50;
    private float _felzenszwalbScale = 100f;
    private int _slicSegmentCount = 250;
    private float _slicCompactness = 10f;
    private int _quickshiftMaxDist = 10;
    private float _quickshiftRatio = 1f;
    private float _bilateralSigma = 30f;
    private float _gaussianSigma = 1f;

    // =====================================================================
    // Properties
    // =====================================================================

    public bool InvertGrayscale
    {
        get => _invertGrayscale;
        set => SetPropertyTriggerRecalculation(ref _invertGrayscale, value);
    }

    public int MarkerCount
    {
        get => _markerCount;
        set => SetPropertyTriggerRecalculation(ref _markerCount, value);
    }

    public int MarkerRadius
    {
        get => _markerRadius;
        set => SetPropertyTriggerRecalculation(ref _markerRadius, value);
    }

    public float GridFitAngle
    {
        get => _gridFitAngle;
        set => SetPropertyTriggerRecalculation(ref _gridFitAngle, value);
    }

    public FilterType SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetCallerPropertyReturn(ref _selectedFilter, value, nameof(SelectedFilter)))
            {
                OnPropertyChanged(nameof(ShowBilateralSigma));
                OnPropertyChanged(nameof(ShowGaussianSigma));
                OnPropertyChanged(nameof(SelectedFilterIndex));
                _ = TriggerRecalculation();
            }
        }
    }

    public int SelectedFilterIndex
    {
        get => (int)_selectedFilter;
        set => SelectedFilter = (FilterType)value;
    }

    public SegmentationMethod SelectedSegmentationMethod
    {
        get => _selectedSegmentationMethod;
        set
        {
            if (SetCallerPropertyReturn(ref _selectedSegmentationMethod, value, nameof(SelectedSegmentationMethod)))
            {
                OnPropertyChanged(nameof(ShowFelzenszwalbParameters));
                OnPropertyChanged(nameof(ShowSlicParameters));
                OnPropertyChanged(nameof(ShowQuickshiftParameters));
                OnPropertyChanged(nameof(ShowWatershedBasedSegmentationInputs));
                OnPropertyChanged(nameof(ShowGridFittingSegmentationInputs));
                OnPropertyChanged(nameof(SelectedSegmentationMethodIndex));
                OnPropertyChanged(nameof(ShowGrayBasedSegmentationInputs));
                _ = TriggerRecalculation();
            }
        }
    }

    public int SelectedSegmentationMethodIndex
    {
        get => (int)_selectedSegmentationMethod;
        set => SelectedSegmentationMethod = (SegmentationMethod)value;
    }

    public bool ShowFelzenszwalbParameters => SelectedSegmentationMethod == SegmentationMethod.Felzenszwalb;
    public bool ShowSlicParameters => SelectedSegmentationMethod == SegmentationMethod.Slic;
    public bool ShowQuickshiftParameters => SelectedSegmentationMethod == SegmentationMethod.Quickshift;
    public bool ShowBilateralSigma => SelectedFilter == FilterType.Bilateral;
    public bool ShowGaussianSigma => SelectedFilter == FilterType.Gaussian;
    public bool ShowWatershedBasedSegmentationInputs => SelectedSegmentationMethod == SegmentationMethod.Watershed;

    public bool ShowGridFittingSegmentationInputs =>
        SelectedSegmentationMethod == SegmentationMethod.BrickFit ||
        SelectedSegmentationMethod == SegmentationMethod.GridFit;

    public bool ShowGrayBasedSegmentationInputs =>
        ShowWatershedBasedSegmentationInputs || ShowGridFittingSegmentationInputs;

    public int FelzenszwalbMinSize
    {
        get => _felzenszwalbMinSize;
        set => SetPropertyTriggerRecalculation(ref _felzenszwalbMinSize, value);
    }

    public float FelzenszwalbScale
    {
        get => _felzenszwalbScale;
        set => SetPropertyTriggerRecalculation(ref _felzenszwalbScale, value);
    }

    public int SlicSegmentCount
    {
        get => _slicSegmentCount;
        set => SetPropertyTriggerRecalculation(ref _slicSegmentCount, value);
    }

    public float SlicCompactness
    {
        get => _slicCompactness;
        set => SetPropertyTriggerRecalculation(ref _slicCompactness, value);
    }

    public int QuickshiftMaxDist
    {
        get => _quickshiftMaxDist;
        set => SetPropertyTriggerRecalculation(ref _quickshiftMaxDist, value);
    }

    public float QuickshiftRatio
    {
        get => _quickshiftRatio;
        set => SetPropertyTriggerRecalculation(ref _quickshiftRatio, value);
    }

    public float BilateralSigma
    {
        get => _bilateralSigma;
        set => SetPropertyTriggerRecalculation(ref _bilateralSigma, value);
    }

    public float GaussianSigma
    {
        get => _gaussianSigma;
        set => SetPropertyTriggerRecalculation(ref _gaussianSigma, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;
        _transitionHelper.InvertGrayscale = _invertGrayscale;
        _transitionHelper.MarkerCount = _markerCount;
        _transitionHelper.MarkerRadius = _markerRadius;
        _transitionHelper.GridFitAngle = _gridFitAngle;
        _transitionHelper.SelectedFilter = _selectedFilter;
        _transitionHelper.SegmentationMethod = _selectedSegmentationMethod;
        _transitionHelper.FelzenszwalbMinSize = _felzenszwalbMinSize;
        _transitionHelper.FelzenszwalbScale = _felzenszwalbScale;
        _transitionHelper.SlicSegmentCount = _slicSegmentCount;
        _transitionHelper.SlicCompactness = _slicCompactness;
        _transitionHelper.QuickshiftMaxDist = _quickshiftMaxDist;
        _transitionHelper.QuickshiftRatio = _quickshiftRatio;
        _transitionHelper.BilateralSigma = _bilateralSigma;
        _transitionHelper.GaussianSigma = _gaussianSigma;
        return true;
    }

    protected override void Recalculate()
    {
        var p1 = Presenters.Pixels1;
        var p2 = Presenters.Pixels2;
        if (p1 == null || p2 == null || p1.Length == 0 || p2.Length == 0)
            return;

        int w = Presenters.Image1.PixelWidth;
        int h = Presenters.Image1.PixelHeight;
        if (w == 0 || h == 0)
            return;

        if (w != Presenters.Image2.PixelWidth || h != Presenters.Image2.PixelHeight)
            return;

        _transitionHelper.Width = w;
        _transitionHelper.Height = h;

        byte[] result = Presenters.IsSmoothMode
            ? _transitionHelper.MixSmooth(p1, p2)
            : _transitionHelper.MixBricks(p1, p2);

        var bmp = _mediaFactory.CreateBitmapFromRaw(w, h, true, result, w * 4);
        Presenters.ResultImage = _mediaFactory.CloneBitmap(bmp);

        if (!Presenters.IsSmoothMode)
        {
            byte[] mapData = _transitionHelper.GetLabelMap();
            if (mapData.Length > 0)
            {
                var labelBmp = _mediaFactory.CreateEmptyBitmap(w, h, true);
                labelBmp.WritePixels(new PixelRect(0, 0, w, h), mapData, w * 4);
                Presenters.LabelMapImage = labelBmp;
            }
        }
    }
}
