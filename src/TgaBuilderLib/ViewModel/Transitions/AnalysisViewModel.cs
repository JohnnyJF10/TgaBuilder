using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class AnalysisViewModel : ThrottledViewModelBase
{
    public AnalysisViewModel(
        ITransitionHelper transitionHelper,
        TransitionInViewModel transitionInVM)
    {
        _transitionHelper = transitionHelper;
        TransitionInVM = transitionInVM;
    }

    public TransitionInViewModel TransitionInVM { get; }

    private ITransitionHelper _transitionHelper;

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

    private bool _invertGrayscale;
    private int _markerCount = 42;
    private int _markerRadius = 5;
    private float _gridFitAngle = 0f;





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
                _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;
                _ = TriggerRecalculation();
            }
        }
    }

    public int SelectedFilterIndex
    {
        get => (int)_selectedFilter;
        set => SelectedFilter = (FilterType)value;
    }

    public int SelectedSegmentationMethodIndex
    {
        get => (int)_selectedSegmentationMethod;
        set => SelectedSegmentationMethod = (SegmentationMethod)value;
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
                _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;
                _ = TriggerRecalculation();
            }
        }
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

    private void ConfigureTransitionHelper()
    {
        _transitionHelper.CurrentBricksPipelineRequirements
        = BricksPipelineRequirements.RequiresAnalysis;

        _transitionHelper.SelectedFilter = SelectedFilter;
        _transitionHelper.SegmentationMethod = SelectedSegmentationMethod;
        _transitionHelper.FelzenszwalbMinSize = FelzenszwalbMinSize;
        _transitionHelper.FelzenszwalbScale = FelzenszwalbScale;
        _transitionHelper.SlicSegmentCount = SlicSegmentCount;
        _transitionHelper.SlicCompactness = SlicCompactness;
        _transitionHelper.QuickshiftMaxDist = QuickshiftMaxDist;
        _transitionHelper.QuickshiftRatio = QuickshiftRatio;
        _transitionHelper.BilateralSigma = BilateralSigma;
        _transitionHelper.GaussianSigma = GaussianSigma;
        _transitionHelper.InvertGrayscale = InvertGrayscale;
        _transitionHelper.MarkerCount = MarkerCount;
        _transitionHelper.MarkerRadius = MarkerRadius;
        _transitionHelper.GridFitAngle = GridFitAngle;
    }

    protected override async Task TriggerRecalculation()
    {
        await _transitionHelper.QueueRecalc(ConfigureTransitionHelper);
    }

}