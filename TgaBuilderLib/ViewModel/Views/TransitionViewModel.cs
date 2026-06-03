using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel;

// =========================================================================
// Enum: identifies which transition pipeline the window is operating in
// =========================================================================

public enum TransitionType
{
    Smooth,
    Bricks,
}

// =========================================================================
// Unified TransitionViewModel — no base class
// =========================================================================

public class TransitionViewModel : ThrottledViewModelBase
{

    private readonly Dictionary<string, BricksPipelineRequirements> _requirementsDict = new()
    {
        { nameof(InvertGrayscale), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(MarkerCount), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(MarkerRadius), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(GridFitAngle), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(SelectedFilter), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(SelectedSegmentationMethod), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(FelzenszwalbMinSize), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(FelzenszwalbScale), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(SlicSegmentCount), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(SlicCompactness), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(QuickshiftMaxDist), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(QuickshiftRatio), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(BilateralSigma), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(GaussianSigma), BricksPipelineRequirements.RequiresAnalysis },
        { nameof(UnderfillingPivot), BricksPipelineRequirements.RequiresSelectionBuilding },
        { nameof(ReverseUnderfilling), BricksPipelineRequirements.RequiresSelectionBuilding },
        { nameof(UnderfillingThreshold), BricksPipelineRequirements.RequiresSelectionBuilding },
        { nameof(BlendMode), BricksPipelineRequirements.RequiresDrawing },
        { nameof(EdgeWidth), BricksPipelineRequirements.RequiresDrawing },
        { nameof(ShadowSize), BricksPipelineRequirements.RequiresDrawing },
        { nameof(ShadowHardness), BricksPipelineRequirements.RequiresDrawing }
    };

    public TransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        IBitmapOperations bitmapOperations,
        PivotViewModel pivotViewModel,
        MainViewModel mainViewModel)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _bitmapOperations = bitmapOperations;
        _mainViewModel = mainViewModel;
    
        PivotVM = pivotViewModel;

        TransitionsPresentersVM = PivotVM.TransitionsPresentersVM;
    }

    // =====================================================================
    // Infrastructure
    // =====================================================================

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly MainViewModel _mainViewModel;
    private readonly IBitmapOperations _bitmapOperations;

    private const int TRANSITIONS_BPP = 4;

    public TransitionsPresentersViewModel TransitionsPresentersVM { get; set; }
    public PivotViewModel PivotVM { get; set; }

    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand? _loadImage1Command;
    private RelayCommand? _loadImage2Command;

    private RelayCommand? _mixCommand;

    private RelayCommand? _markFinishedCommand;
    private RelayCommand? _applyCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand MixCommand => _mixCommand ??= new RelayCommand(Mix);

    public ICommand LoadImage1Command => _loadImage1Command 
        ??= new RelayCommand(() => TransitionsPresentersVM.LoadImage1(_mainViewModel.Selection.Presenter));
    public ICommand LoadImage2Command => _loadImage2Command 
        ??= new RelayCommand(() => TransitionsPresentersVM.LoadImage2(_mainViewModel.Selection.Presenter));

    public ICommand MarkFinishedCommand => _markFinishedCommand ??= new RelayCommand(MarkFinished);
    public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(Apply);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);


    // =====================================================================
    // Brick-mode properties
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
    private float _underfillingPivot = 0.5f;
    private bool _reverseUnderfilling = false;
    private int _underFillingThreshold = 0;
    private EdgeBlendMode _blendMode = EdgeBlendMode.Multiply;
    private int _edgeWidth = 1;
    private int _shadowSize = 3;
    private int _shadowHardness = 50;

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

    public float UnderfillingPivot
    {
        get => _underfillingPivot;
        set => SetPropertyTriggerRecalculation(ref _underfillingPivot, value);
    }

    public bool ReverseUnderfilling
    {
        get => _reverseUnderfilling;
        set => SetPropertyTriggerRecalculation(ref _reverseUnderfilling, value);
    }

    public int UnderfillingThreshold
    {
        get => _underFillingThreshold;
        set => SetPropertyTriggerRecalculation(ref _underFillingThreshold, value);
    }

    public EdgeBlendMode BlendMode
    {
        get => _blendMode;
        set => SetPropertyTriggerRecalculation(ref _blendMode, value);
    }

    public Array EdgeBlendModes => Enum.GetValues(typeof(EdgeBlendMode));

    public int EdgeWidth
    {
        get => _edgeWidth;
        set => SetPropertyTriggerRecalculation(ref _edgeWidth, value);
    }

    public int ShadowSize
    {
        get => _shadowSize;
        set => SetPropertyTriggerRecalculation(ref _shadowSize, value);
    }

    public int ShadowHardness
    {
        get => _shadowHardness;
        set => SetPropertyTriggerRecalculation(ref _shadowHardness, value);
    }

    public int SelectedSegmentationMethodIndex
    {
        get => (int)_selectedSegmentationMethod;
        set => SelectedSegmentationMethod = (SegmentationMethod)value;
    }





    // =====================================================================
    // Pipeline logic
    // =====================================================================



    private void ConfigureTransitionHelper()
    {
        _transitionHelper.InvertGrayscale = InvertGrayscale;
        _transitionHelper.MarkerCount = MarkerCount;
        _transitionHelper.MarkerRadius = MarkerRadius;
        _transitionHelper.GridFitAngle = GridFitAngle;
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
        _transitionHelper.UnderfillingPivot = UnderfillingPivot;
        _transitionHelper.ReverseUnderfilling = ReverseUnderfilling;
        _transitionHelper.UnderfillingThreshold = UnderfillingThreshold;
        _transitionHelper.EdgeColor = TransitionsPresentersVM.EdgeColor;
        _transitionHelper.BlendMode = BlendMode;
        _transitionHelper.EdgeWidth = EdgeWidth;
        _transitionHelper.ShadowColor = TransitionsPresentersVM.ShadowColor;
        _transitionHelper.ShadowSize = ShadowSize;
        _transitionHelper.ShadowHardness = ShadowHardness;
    }

    private void Mix()
    {
        ConfigureTransitionHelper();
        TransitionsPresentersVM.Mix();
    }



    private void MarkFinished()
    {
        _transitionHelper.CleanUp();
        _mainViewModel.IsTransitionViewOpen = false;
    }

    private void Apply()
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(TransitionsPresentersVM.ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
    }

    private void OK(IView view)
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(TransitionsPresentersVM.ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
        MarkFinished();
        view.CloseAsync();
    }

    private void Cancel(IView view)
    {
        MarkFinished();
        view.CloseAsync();
    }


    protected override bool PreProcess()
    {
        return TransitionsPresentersVM.DoPreProcessing();
    }

    protected override async Task Recalculate()
    {
        ConfigureTransitionHelper();

        await TransitionsPresentersVM.DoRecalculation();

    }

    protected override void SetPropertyTriggerRecalculation<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;

            if (_requirementsDict.TryGetValue(propertyName ?? string.Empty, out var requirements))
                _transitionHelper.CurrentBricksPipelineRequirements = requirements;

            OnPropertyChanged(propertyName ?? string.Empty);
            _ = TriggerRecalculation();
        }
    }
}
