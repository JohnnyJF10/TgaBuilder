using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;
using TgaBuilderLib.ViewModel.Transitions;
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

public class TransitionViewModel : ViewModelBase
{
    public TransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        IBitmapOperations bitmapOperations,
        MainViewModel mainViewModel,
        TransitionPresentersViewModel presenters,
        TransitionAnalysisViewModel analysis,
        TransitionPivotSmoothViewModel pivotSmooth,
        TransitionPivotBricksViewModel pivotBricks,
        TransitionUnderfillingViewModel underfilling,
        TransitionEdgeViewModel edge,
        TransitionShadowViewModel shadow,
        TransitionManualViewModel manual)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _bitmapOperations = bitmapOperations;
        _mainViewModel = mainViewModel;

        Presenters = presenters;
        Analysis = analysis;
        PivotSmooth = pivotSmooth;
        PivotBricks = pivotBricks;
        Underfilling = underfilling;
        Edge = edge;
        Shadow = shadow;
        Manual = manual;

        _pixels1 = new byte[64 * 64 * 4];
        _pixels2 = new byte[64 * 64 * 4];

        Presenters.Pixels1 = _pixels1;
        Presenters.Pixels2 = _pixels2;
        Presenters.IsSmoothMode = IsSmoothMode;
        Presenters.PropertyChanged += Presenters_PropertyChanged;

        Edge.PropertyChanged += Edge_PropertyChanged;
        Shadow.PropertyChanged += Shadow_PropertyChanged;

        SyncSharedControlsFromActiveChild();
    }

    // =====================================================================
    // Infrastructure
    // =====================================================================

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly MainViewModel _mainViewModel;
    private readonly IBitmapOperations _bitmapOperations;

    // =====================================================================
    // Child View Models
    // =====================================================================

    public TransitionPresentersViewModel Presenters { get; }
    public TransitionAnalysisViewModel Analysis { get; }
    public TransitionPivotSmoothViewModel PivotSmooth { get; }
    public TransitionPivotBricksViewModel PivotBricks { get; }
    public TransitionUnderfillingViewModel Underfilling { get; }
    public TransitionEdgeViewModel Edge { get; }
    public TransitionShadowViewModel Shadow { get; }
    public TransitionManualViewModel Manual { get; }

    private const int TRANSITIONS_BPP = 4;

    private readonly object _pivotLock = new();
    private bool _pivotUpdateRunning;
    private bool _pivotUpdatePending;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private byte[] _pixels1;
    private byte[] _pixels2;
    private bool _isLabelMapExpanded;

    public IVisualInvalidator? VisualInvalidator { get; set; }

    protected byte[] Pixels1 => _pixels1;
    protected byte[] Pixels2 => _pixels2;

    public IWriteableBitmap Image1
    {
        get => Presenters.Image1;
        set
        {
            Presenters.Image1 = value;
            OnPropertyChanged(nameof(Image1));
        }
    }

    public IWriteableBitmap Image2
    {
        get => Presenters.Image2;
        set
        {
            Presenters.Image2 = value;
            OnPropertyChanged(nameof(Image2));
        }
    }

    public IWriteableBitmap ResultImage
    {
        get => Presenters.ResultImage;
        set
        {
            Presenters.ResultImage = value;
            OnPropertyChanged(nameof(ResultImage));
        }
    }

    public bool InitTextVisible
    {
        get => Presenters.InitTextVisible;
        set
        {
            Presenters.InitTextVisible = value;
            OnPropertyChanged(nameof(InitTextVisible));
        }
    }

    public IWriteableBitmap? LabelMapImage
    {
        get => Presenters.LabelMapImage;
        set
        {
            Presenters.LabelMapImage = value;
            OnPropertyChanged(nameof(LabelMapImage));
        }
    }

    public IWriteableBitmap? IndicatorMapImage
    {
        get => Presenters.IndicatorMapImage;
        set
        {
            Presenters.IndicatorMapImage = value;
            OnPropertyChanged(nameof(IndicatorMapImage));
        }
    }

    public bool IsIndicatorMapVisible
    {
        get => Presenters.IsIndicatorMapVisible;
        set
        {
            Presenters.IsIndicatorMapVisible = value;
            OnPropertyChanged(nameof(IsIndicatorMapVisible));
        }
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand? _loadImage1Command;
    private RelayCommand? _loadImage2Command;
    private RelayCommand? _swapImagesCommand;
    private RelayCommand? _mixCommand;
    private RelayCommand<(int, int)>? _setExplicitTileVisibilityCommand;
    private RelayCommand? _resetExplicitVisibilityCommand;
    private RelayCommand<(int, int)>? _requestLabelIndicatorCommand;
    private RelayCommand? _markFinishedCommand;
    private RelayCommand? _applyCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand MixCommand => _mixCommand ??= new RelayCommand(Mix);
    public ICommand LoadImage1Command => _loadImage1Command ??= new RelayCommand(LoadImage1);
    public ICommand LoadImage2Command => _loadImage2Command ??= new RelayCommand(LoadImage2);
    public ICommand SwapImagesCommand => _swapImagesCommand ??= new RelayCommand(SwapImages);
    public ICommand SetExplicitTileVisibilityCommand => _setExplicitTileVisibilityCommand
        ??= new RelayCommand<(int, int)>(args =>
        SetExplicitTileVisibility(args.Item1, args.Item2));
    public ICommand ResetExplicitVisibilityCommand => _resetExplicitVisibilityCommand
        ??= new RelayCommand(ResetAllExplicitTileVisibility);
    public ICommand RequestLabelIndicatorCommand => _requestLabelIndicatorCommand
        ??= new RelayCommand<(int, int)>(args => RequestNewIndicatorMapImage(args.Item1, args.Item2));
    public ICommand MarkFinishedCommand => _markFinishedCommand ??= new RelayCommand(MarkFinished);
    public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(Apply);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);

    // =====================================================================
    // Shared pivot / transition-mode properties
    // =====================================================================

    private TransitionMode _selectedTransitionMode = TransitionMode.Top;
    private float _pivotValue = 0.5f;
    private Color _colorSource = new(0, 0, 0, 0);
    private Color _colorTarget = new(0, 0, 0, 0);

    public TransitionMode SelectedTransitionMode
    {
        get => _selectedTransitionMode;
        set
        {
            if (_selectedTransitionMode == value)
                return;

            _selectedTransitionMode = value;
            OnPropertyChanged(nameof(SelectedTransitionMode));

            if (IsSmoothMode)
                PivotSmooth.SelectedTransitionMode = value;
            else
                PivotBricks.SelectedTransitionMode = value;
        }
    }

    public float PivotValue
    {
        get => _pivotValue;
        set
        {
            if (_pivotValue == value)
                return;

            _pivotValue = value;
            OnPropertyChanged(nameof(PivotValue));

            if (IsSmoothMode)
                PivotSmooth.PivotValue = value;
            else
                PivotBricks.PivotValue = value;
        }
    }

    public Color ColorSource
    {
        get => _colorSource;
        set => SetCallerProperty(ref _colorSource, value);
    }

    public Color ColorTarget
    {
        get => _colorTarget;
        set => SetCallerProperty(ref _colorTarget, value);
    }

    // =====================================================================
    // Mode selection (TransitionType enum)
    // =====================================================================

    private TransitionType _selectedTransitionType = TransitionType.Bricks;

    public TransitionType SelectedTransitionType
    {
        get => _selectedTransitionType;
        set
        {
            if (_selectedTransitionType == value)
                return;

            _selectedTransitionType = value;
            OnPropertyChanged(nameof(SelectedTransitionType));
            OnPropertyChanged(nameof(IsSmoothMode));
            OnPropertyChanged(nameof(IsBrickMode));

            Presenters.IsSmoothMode = IsSmoothMode;
            SyncSharedControlsFromActiveChild();

            if (IsBrickMode)
                _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

            if (IsSmoothMode)
            {
                IsLabelMapExpanded = false;
                Manual.IsExplicitTileVisibilityEraseMode = false;
                Manual.IsExplicitTileVisibilityDrawMode = false;
                Edge.IsEyedropperMode = false;
                Shadow.IsShadowEyedropperMode = false;
                IsIndicatorMapVisible = false;
            }

            _ = TriggerRecalculation();
        }
    }

    // Computed convenience helpers (used by visibility bindings in the views)
    public bool IsSmoothMode => _selectedTransitionType == TransitionType.Smooth;
    public bool IsBrickMode => _selectedTransitionType == TransitionType.Bricks;

    private RelayCommand<(int X, int Y, int imageNum)>? _mouseOverCommand;

    public ICommand MouseOverCommand => _mouseOverCommand
        ??= new RelayCommand<(int X, int Y, int imageNum)>(MouseOverImages);

    private void MouseOverImages((int X, int Y, int imageNum) args)
    {
        if (Edge.IsEyedropperMode || Shadow.IsShadowEyedropperMode)
            DoColorPicking(args.X, args.Y, args.imageNum, Shadow.IsShadowEyedropperMode);
    }

    private void DoColorPicking(int x, int y, int imageNum, bool pickShadowColor)
    {
        var sampledColor = _bitmapOperations.GetPixelBrush(imageNum == 1 ? Image1 : Image2, x, y);
        if (pickShadowColor)
            Shadow.ShadowColor = sampledColor;
        else
            Edge.EdgeColor = sampledColor;
    }

    // =====================================================================
    // Pipeline logic
    // =====================================================================

    private byte[] CreateMixedPixels()
        => IsSmoothMode
            ? _transitionHelper.MixSmooth(Pixels1, Pixels2)
            : _transitionHelper.MixBricks(Pixels1, Pixels2);

    private void ConfigureTransitionHelperCore()
    {
        if (IsSmoothMode)
        {
            _transitionHelper.Hardness = PivotSmooth.BlendHardnessValue;
        }
        else
        {
            _transitionHelper.InvertGrayscale = Analysis.InvertGrayscale;
            _transitionHelper.ReversePivot = PivotBricks.ReversePivot;
            _transitionHelper.SliceCornerTiles = PivotBricks.SliceCornerTiles;
            _transitionHelper.ProtectEdges = PivotBricks.ProtectEdges;
            _transitionHelper.MarkerCount = Analysis.MarkerCount;
            _transitionHelper.MarkerRadius = Analysis.MarkerRadius;
            _transitionHelper.GridFitAngle = Analysis.GridFitAngle;
            _transitionHelper.SelectedFilter = Analysis.SelectedFilter;
            _transitionHelper.SegmentationMethod = Analysis.SelectedSegmentationMethod;
            _transitionHelper.FelzenszwalbMinSize = Analysis.FelzenszwalbMinSize;
            _transitionHelper.FelzenszwalbScale = Analysis.FelzenszwalbScale;
            _transitionHelper.SlicSegmentCount = Analysis.SlicSegmentCount;
            _transitionHelper.SlicCompactness = Analysis.SlicCompactness;
            _transitionHelper.QuickshiftMaxDist = Analysis.QuickshiftMaxDist;
            _transitionHelper.QuickshiftRatio = Analysis.QuickshiftRatio;
            _transitionHelper.BilateralSigma = Analysis.BilateralSigma;
            _transitionHelper.GaussianSigma = Analysis.GaussianSigma;
            _transitionHelper.UnderfillingPivot = Underfilling.UnderfillingPivot;
            _transitionHelper.ReverseUnderfilling = Underfilling.ReverseUnderfilling;
            _transitionHelper.UnderfillingThreshold = Underfilling.UnderfillingThreshold;
            _transitionHelper.EdgeColor = Edge.EdgeColor;
            _transitionHelper.BlendMode = Edge.BlendMode;
            _transitionHelper.EdgeWidth = Edge.EdgeWidth;
            _transitionHelper.ShadowColor = Shadow.ShadowColor;
            _transitionHelper.ShadowSize = Shadow.ShadowSize;
            _transitionHelper.ShadowHardness = Shadow.ShadowHardness;
        }
    }

    private void SwapImages()
    {
        if (IsBrickMode)
            _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

        var tempImage = Image1;
        Image1 = Image2;
        Image2 = tempImage;

        var tempPixels = _pixels1;
        _pixels1 = _pixels2;
        _pixels2 = tempPixels;
        Presenters.Pixels1 = _pixels1;
        Presenters.Pixels2 = _pixels2;

        _ = TriggerRecalculation();
    }

    private void Mix()
    {
        if (IsBrickMode)
            _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

        if (!CompareInputSpecs())
            return;

        ConfigureTransitionHelper(Image1.PixelWidth, Image1.PixelHeight);
        UpdateResultImages(Image1.PixelWidth, Image1.PixelHeight, CreateMixedPixels());
    }

    private void LoadImage1()
    {
        Image1 = _mainViewModel.Selection.Presenter.HasAlpha
            ? _mediaFactory.CloneBitmap(_mainViewModel.Selection.Presenter)
            : _bitmapOperations.ConvertRGB24ToBGRA32(_mainViewModel.Selection.Presenter);

        _pixels1 = new byte[Image1.PixelWidth * Image1.PixelHeight * TRANSITIONS_BPP];
        Image1.CopyPixels(_pixels1, Image1.PixelWidth * TRANSITIONS_BPP, 0);
        Presenters.Pixels1 = _pixels1;
        InitTextVisible = false;

        _transitionHelper.CleanUp();
    }

    private void LoadImage2()
    {
        Image2 = _mainViewModel.Selection.Presenter.HasAlpha
            ? _mediaFactory.CloneBitmap(_mainViewModel.Selection.Presenter)
            : _bitmapOperations.ConvertRGB24ToBGRA32(_mainViewModel.Selection.Presenter);

        _pixels2 = new byte[Image2.PixelWidth * Image2.PixelHeight * TRANSITIONS_BPP];
        Image2.CopyPixels(_pixels2, Image2.PixelWidth * TRANSITIONS_BPP, 0);
        Presenters.Pixels2 = _pixels2;
        InitTextVisible = false;

        _transitionHelper.CleanUp();
    }

    private bool CompareInputSpecs()
        => Image1.PixelWidth == Image2.PixelWidth &&
           Image1.PixelHeight == Image2.PixelHeight &&
           Image1.HasAlpha == Image2.HasAlpha;

    private void ConfigureTransitionHelper(int width, int height)
    {
        _transitionHelper.Width = width;
        _transitionHelper.Height = height;
        _transitionHelper.Mode = SelectedTransitionMode;
        _transitionHelper.Pivot = PivotValue;
        _transitionHelper.Widening = IsSmoothMode ? PivotSmooth.WideningValue : PivotBricks.WideningValue;
        _transitionHelper.Shift = IsSmoothMode ? PivotSmooth.ShiftValue : PivotBricks.ShiftValue;

        ConfigureTransitionHelperCore();
    }

    private void MarkFinished()
    {
        _transitionHelper.CleanUp();
        _mainViewModel.IsTransitionViewOpen = false;
    }

    private void Apply()
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
    }

    private void OK(IView view)
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
        MarkFinished();
        view.CloseAsync();
    }

    private void Cancel(IView view)
    {
        MarkFinished();
        view.CloseAsync();
    }

    private void UpdateResultImages(int width, int height, byte[] resultPixels)
    {
        var resultBitmap = _mediaFactory.CreateBitmapFromRaw(width, height, true, resultPixels, width * 4);
        ResultImage = _mediaFactory.CloneBitmap(resultBitmap);

        if (IsSmoothMode)
            return;

        byte[] mapData = _transitionHelper.GetLabelMap();
        if (mapData.Length == 0)
            return;

        var labelBmp = _mediaFactory.CreateEmptyBitmap(width, height, true);
        labelBmp.WritePixels(new PixelRect(0, 0, width, height), mapData, width * 4);
        LabelMapImage = labelBmp;
    }

    private void RequestNewIndicatorMapImage(int x, int y)
    {
        int label = _transitionHelper.GetLabelAtPixel(x, y);

        if (label == 0)
            return;

        byte[] mapData = _transitionHelper.GetTileIndicator(label);

        int mapW = ResultImage?.PixelWidth ?? 0;
        int mapH = ResultImage?.PixelHeight ?? 0;

        if (mapData.Length == 0 || mapW == 0 || mapH == 0)
            return;

        var indicatorBmp = _mediaFactory.CreateEmptyBitmap(mapW, mapH, true);
        indicatorBmp.WritePixels(
            new PixelRect(0, 0, mapW, mapH),
            mapData,
            mapW * 4);

        IndicatorMapImage = indicatorBmp;
    }

    private void SetExplicitTileVisibility(int x, int y)
    {
        if (!Manual.IsExplicitTileVisibilityEraseMode && !Manual.IsExplicitTileVisibilityDrawMode)
            return;

        int label = _transitionHelper.GetLabelAtPixel(x, y);

        if (label == 0)
            return;

        bool visibilityChanged = _transitionHelper.SetExplicitTileVisibility(label, Manual.IsExplicitTileVisibilityDrawMode);
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;

        if (visibilityChanged)
            _ = TriggerRecalculation();
    }

    private void ResetAllExplicitTileVisibility()
    {
        _transitionHelper.ResetAllExplicitTileVisibility();
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;
        _ = TriggerRecalculation();
    }

    // =====================================================================
    // Recalculation helpers
    // =====================================================================

    protected async Task TriggerRecalculation()
    {
        lock (_pivotLock)
        {
            if (_pivotUpdateRunning)
            {
                _pivotUpdatePending = true;
                return;
            }

            _pivotUpdateRunning = true;
        }

        try
        {
            do
            {
                lock (_pivotLock)
                {
                    _pivotUpdatePending = false;
                }

                if (!CompareInputSpecs())
                    return;

                int width = Image1.PixelWidth;
                int height = Image1.PixelHeight;

                await Task.Delay(50);

                ConfigureTransitionHelper(width, height);

                byte[] resultPixels = await Task.Run(CreateMixedPixels);
                UpdateResultImages(width, height, resultPixels);
            }
            while (_pivotUpdatePending);
        }
        finally
        {
            lock (_pivotLock)
            {
                _pivotUpdateRunning = false;
            }
        }
    }

    private void SyncSharedControlsFromActiveChild()
    {
        if (IsSmoothMode)
        {
            _selectedTransitionMode = PivotSmooth.SelectedTransitionMode;
            _pivotValue = PivotSmooth.PivotValue;
        }
        else
        {
            _selectedTransitionMode = PivotBricks.SelectedTransitionMode;
            _pivotValue = PivotBricks.PivotValue;
        }

        OnPropertyChanged(nameof(SelectedTransitionMode));
        OnPropertyChanged(nameof(PivotValue));
    }

    private void Presenters_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TransitionPresentersViewModel.Image1):
                OnPropertyChanged(nameof(Image1));
                break;
            case nameof(TransitionPresentersViewModel.Image2):
                OnPropertyChanged(nameof(Image2));
                break;
            case nameof(TransitionPresentersViewModel.ResultImage):
                OnPropertyChanged(nameof(ResultImage));
                break;
            case nameof(TransitionPresentersViewModel.LabelMapImage):
                OnPropertyChanged(nameof(LabelMapImage));
                break;
            case nameof(TransitionPresentersViewModel.IndicatorMapImage):
                OnPropertyChanged(nameof(IndicatorMapImage));
                break;
            case nameof(TransitionPresentersViewModel.IsIndicatorMapVisible):
                OnPropertyChanged(nameof(IsIndicatorMapVisible));
                break;
            case nameof(TransitionPresentersViewModel.InitTextVisible):
                OnPropertyChanged(nameof(InitTextVisible));
                break;
        }
    }

    private void Edge_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransitionEdgeViewModel.IsEyedropperMode) && Edge.IsEyedropperMode)
            Shadow.IsShadowEyedropperMode = false;
    }

    private void Shadow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransitionShadowViewModel.IsShadowEyedropperMode) && Shadow.IsShadowEyedropperMode)
            Edge.IsEyedropperMode = false;
    }
}
