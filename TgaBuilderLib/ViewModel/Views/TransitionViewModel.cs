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

public class TransitionViewModel : ViewModelBase
{
    public TransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        IBitmapOperations bitmapOperations,
        MainViewModel mainViewModel)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _bitmapOperations = bitmapOperations;
        _mainViewModel = mainViewModel;

        _image1 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _image2 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _resultImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);

        _pixels1 = new byte[64 * 64 * 4];
        _pixels2 = new byte[64 * 64 * 4];
    }

    // =====================================================================
    // Infrastructure
    // =====================================================================

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly MainViewModel _mainViewModel;
    private readonly IBitmapOperations _bitmapOperations;

    private const int TRANSITIONS_BPP = 4;

    private readonly object _pivotLock = new();
    private bool _pivotUpdateRunning;
    private bool _pivotUpdatePending;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private IWriteableBitmap _image1;
    private IWriteableBitmap _image2;
    private IWriteableBitmap _resultImage;
    private byte[] _pixels1;
    private byte[] _pixels2;

    private bool _initTextVisible = true;

    public IVisualInvalidator? VisualInvalidator { get; set; }

    protected byte[] Pixels1 => _pixels1;
    protected byte[] Pixels2 => _pixels2;

    public IWriteableBitmap Image1
    {
        get => _image1;
        set => SetCallerProperty(ref _image1, value);
    }

    public IWriteableBitmap Image2
    {
        get => _image2;
        set => SetCallerProperty(ref _image2, value);
    }

    public IWriteableBitmap ResultImage
    {
        get => _resultImage;
        set => SetCallerProperty(ref _resultImage, value);
    }

    public bool InitTextVisible
    {
        get => _initTextVisible;
        set => SetCallerProperty(ref _initTextVisible, value);
    }

    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand? _loadImage1Command;
    private RelayCommand? _loadImage2Command;
    private RelayCommand? _swapImagesCommand;
    private RelayCommand? _mixCommand;
    private RelayCommand? _markFinishedCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand MixCommand => _mixCommand ??= new RelayCommand(Mix);
    public ICommand LoadImage1Command => _loadImage1Command ??= new RelayCommand(LoadImage1);
    public ICommand LoadImage2Command => _loadImage2Command ??= new RelayCommand(LoadImage2);
    public ICommand SwapImagesCommand => _swapImagesCommand ??= new RelayCommand(SwapImages);
    public ICommand MarkFinishedCommand => _markFinishedCommand ??= new RelayCommand(MarkFinished);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);

    // =====================================================================
    // Shared pivot / transition-mode properties
    // =====================================================================

    private TransitionMode _selectedTransitionMode = TransitionMode.Top;
    private float _pivotValue = 0.5f;
    private float _wideningValue = 0f;
    private float _shiftValue = 0f;
    private Color _colorSource = new(0, 0, 0, 0);
    private Color _colorTarget = new(0, 0, 0, 0);

    public TransitionMode SelectedTransitionMode
    {
        get => _selectedTransitionMode;
        set => SetPropertyTriggerRecalculation(ref _selectedTransitionMode, value,
            BricksPipelineRequirements.RequiresSelectionBuilding);
    }

    public float PivotValue
    {
        get => _pivotValue;
        set => SetPropertyTriggerRecalculation(ref _pivotValue, value,
            BricksPipelineRequirements.RequiresSelectionBuilding);
    }

    public float WideningValue
    {
        get => _wideningValue;
        set => SetPropertyTriggerRecalculation(ref _wideningValue, value,
            BricksPipelineRequirements.RequiresSelectionBuilding);
    }

    public float ShiftValue
    {
        get => _shiftValue;
        set => SetPropertyTriggerRecalculation(ref _shiftValue, value,
            BricksPipelineRequirements.RequiresSelectionBuilding);
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

    private TransitionType _selectedTransitionType = TransitionType.Smooth;

    public TransitionType SelectedTransitionType
    {
        get => _selectedTransitionType;
        set
        {
            if (_selectedTransitionType != value)
            {
                _selectedTransitionType = value;
                OnPropertyChanged(nameof(SelectedTransitionType));
                OnPropertyChanged(nameof(IsSmoothMode));
                OnPropertyChanged(nameof(IsBrickMode));

                if (_selectedTransitionType == TransitionType.Bricks)
                    _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;

                _ = TriggerRecalculation();
            }
        }
    }

    // Computed convenience helpers (used by visibility bindings in the views)
    public bool IsSmoothMode => _selectedTransitionType == TransitionType.Smooth;
    public bool IsBrickMode => _selectedTransitionType == TransitionType.Bricks;

    // =====================================================================
    // Smooth-mode properties
    // =====================================================================

    private float _blendHardnessValue = 0.5f;

    public float BlendHardnessValue
    {
        get => _blendHardnessValue;
        set => SetPropertyTriggerRecalculation(ref _blendHardnessValue, value);
    }

    // =====================================================================
    // Brick-mode properties
    // =====================================================================

    private IWriteableBitmap? _labelMapImage;
    private bool _invertGrayscale;
    private int _markerRadius = 3;
    private bool _reversePivot;
    private bool _sliceCornerTiles;
    private bool _protectEdges = true;
    private bool _isLabelMapExpanded;
    private FilterType _selectedFilter = FilterType.BoxBlur;
    private SegmentationMethod _selectedSegmentationMethod = SegmentationMethod.Felzenszwalb;
    private int _felzenszwalbMinSize = 50;
    private float _felzenszwalbScale = 100f;
    private int _slicSegmentCount = 250;
    private float _slicCompactness = 10f;
    private int _quickshiftMaxDist = 10;
    private float _quickshiftRatio = 1f;
    private Color _edgeColor = new Color(255, 255, 255, 128);
    private EdgeBlendMode _blendMode = EdgeBlendMode.Multiply;
    private int _edgeWidth = 1;
    private bool _isEyedropperMode;
    private BricksPipelineRequirements _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;

    private RelayCommand<(int X, int Y, int imageNum)>? _mouseOverCommand;

    public IWriteableBitmap? LabelMapImage
    {
        get => _labelMapImage;
        set => SetCallerProperty(ref _labelMapImage, value);
    }

    public bool InvertGrayscale
    {
        get => _invertGrayscale;
        set => SetPropertyTriggerRecalculation(ref _invertGrayscale, value,
            BricksPipelineRequirements.RequiresAnalysis, null);
    }

    public int MarkerRadius
    {
        get => _markerRadius;
        set => SetPropertyTriggerRecalculation(ref _markerRadius, value,
            BricksPipelineRequirements.RequiresAnalysis);
    }

    public bool ReversePivot
    {
        get => _reversePivot;
        set => SetPropertyTriggerRecalculation(ref _reversePivot, value,
            BricksPipelineRequirements.RequiresSelectionBuilding, null);
    }

    public bool SliceCornerTiles
    {
        get => _sliceCornerTiles;
        set => SetPropertyTriggerRecalculation(ref _sliceCornerTiles, value,
            BricksPipelineRequirements.RequiresSelectionBuilding, null);
    }

    public bool ProtectEdges
    {
        get => _protectEdges;
        set => SetPropertyTriggerRecalculation(ref _protectEdges, value,
            BricksPipelineRequirements.RequiresSelectionBuilding, null);
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    public FilterType SelectedFilter
    {
        get => _selectedFilter;
        set => SetPropertyTriggerRecalculation(ref _selectedFilter, value,
            BricksPipelineRequirements.RequiresAnalysis, null);
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
                OnPropertyChanged(nameof(ShowGrayBasedSegmentationInputs));
                OnPropertyChanged(nameof(SelectedSegmentationMethodIndex));
                _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;
                _ = TriggerRecalculation();
            }
        }
    }

    public bool ShowFelzenszwalbParameters => SelectedSegmentationMethod == SegmentationMethod.Felzenszwalb;
    public bool ShowSlicParameters => SelectedSegmentationMethod == SegmentationMethod.Slic;
    public bool ShowQuickshiftParameters => SelectedSegmentationMethod == SegmentationMethod.Quickshift;
    public bool ShowGrayBasedSegmentationInputs =>
        SelectedSegmentationMethod == SegmentationMethod.Watershed
        || SelectedSegmentationMethod == SegmentationMethod.XYProjection
        || SelectedSegmentationMethod == SegmentationMethod.YXProjection;

    public int FelzenszwalbMinSize
    {
        get => _felzenszwalbMinSize;
        set => SetPropertyTriggerRecalculation(ref _felzenszwalbMinSize, value,
            BricksPipelineRequirements.RequiresAnalysis);
    }

    public float FelzenszwalbScale
    {
        get => _felzenszwalbScale;
        set => SetPropertyTriggerRecalculation(ref _felzenszwalbScale, value,
            BricksPipelineRequirements.RequiresAnalysis);
    }

    public int SlicSegmentCount
    {
        get => _slicSegmentCount;
        set => SetPropertyTriggerRecalculation(ref _slicSegmentCount, value,
            BricksPipelineRequirements.RequiresAnalysis);
    }

    public float SlicCompactness
    {
        get => _slicCompactness;
        set => SetPropertyTriggerRecalculation(ref _slicCompactness, value,
            BricksPipelineRequirements.RequiresAnalysis);
    }

    public int QuickshiftMaxDist
    {
        get => _quickshiftMaxDist;
        set => SetPropertyTriggerRecalculation(ref _quickshiftMaxDist, value,
            BricksPipelineRequirements.RequiresAnalysis);
    }

    public float QuickshiftRatio
    {
        get => _quickshiftRatio;
        set => SetPropertyTriggerRecalculation(ref _quickshiftRatio, value,
            BricksPipelineRequirements.RequiresAnalysis);
    }

    public Color EdgeColor
    {
        get => _edgeColor;
        set => SetPropertyTriggerRecalculation(ref _edgeColor, value,
            BricksPipelineRequirements.RequiresEdgeColoring);
    }

    public EdgeBlendMode BlendMode
    {
        get => _blendMode;
        set => SetPropertyTriggerRecalculation(ref _blendMode, value,
            BricksPipelineRequirements.RequiresEdgeColoring);
    }

    public Array EdgeBlendModes => Enum.GetValues(typeof(EdgeBlendMode));

    public int EdgeWidth
    {
        get => _edgeWidth;
        set => SetPropertyTriggerRecalculation(ref _edgeWidth, value,
            BricksPipelineRequirements.RequiresEdgeColoring);
    }

    public bool IsEyedropperMode
    {
        get => _isEyedropperMode;
        set => SetProperty(ref _isEyedropperMode, value, nameof(IsEyedropperMode));
    }

    public int SelectedSegmentationMethodIndex
    {
        get => (int)_selectedSegmentationMethod;
        set => SelectedSegmentationMethod = (SegmentationMethod)value;
    }

    public ICommand MouseOverCommand => _mouseOverCommand
        ??= new RelayCommand<(int X, int Y, int imageNum)>(MouseOverImages);

    private void MouseOverImages((int X, int Y, int imageNum) args)
    {
        if (IsEyedropperMode)
            DoColorPicking(args.X, args.Y, args.imageNum);
    }

    private void DoColorPicking(int x, int y, int imageNum)
    {
        EdgeColor = _bitmapOperations.GetPixelBrush(imageNum == 1 ? Image1 : Image2, x, y);
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
            _transitionHelper.Hardness = _blendHardnessValue;
        }
        else
        {
            _transitionHelper.CurrentBricksPipelineRequirements = _currentRequirements;
            _transitionHelper.InvertGrayscale = InvertGrayscale;
            _transitionHelper.ReversePivot = ReversePivot;
            _transitionHelper.SliceCornerTiles = SliceCornerTiles;
            _transitionHelper.ProtectEdges = ProtectEdges;
            _transitionHelper.MarkerRadius = MarkerRadius;
            _transitionHelper.SelectedFilter = SelectedFilter;
            _transitionHelper.SegmentationMethod = SelectedSegmentationMethod;
            _transitionHelper.FelzenszwalbMinSize = FelzenszwalbMinSize;
            _transitionHelper.FelzenszwalbScale = FelzenszwalbScale;
            _transitionHelper.SlicSegmentCount = SlicSegmentCount;
            _transitionHelper.SlicCompactness = SlicCompactness;
            _transitionHelper.QuickshiftMaxDist = QuickshiftMaxDist;
            _transitionHelper.QuickshiftRatio = QuickshiftRatio;
            _transitionHelper.EdgeColor = EdgeColor;
            _transitionHelper.BlendMode = BlendMode;
            _transitionHelper.EdgeWidth = EdgeWidth;
        }
    }

    private void OnResultUpdated()
    {
        if (IsBrickMode)
            UpdateLabelMapImage();
    }

    private void SwapImages()
    {
        if (IsBrickMode)
            _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;

        var tempImage = Image1;
        Image1 = Image2;
        Image2 = tempImage;

        var tempPixels = _pixels1;
        _pixels1 = _pixels2;
        _pixels2 = tempPixels;

        _ = TriggerRecalculation();
    }

    private void Mix()
    {
        if (IsBrickMode)
            _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;

        if (!CompareInputSpecs())
            return;

        ConfigureTransitionHelper(Image1.PixelWidth, Image1.PixelHeight);

        var resultPixels = CreateMixedPixels();

        ResultImage = _mediaFactory.CreateEmptyBitmap(Image1.PixelWidth, Image1.PixelHeight, Image1.HasAlpha);
        ResultImage.WritePixels(
            new PixelRect(0, 0, ResultImage.PixelWidth, ResultImage.PixelHeight),
            resultPixels,
            ResultImage.PixelWidth * TRANSITIONS_BPP);

        OnResultUpdated();
    }

    private void LoadImage1()
    {
        Image1 = _mainViewModel.Selection.Presenter.HasAlpha
            ? _mediaFactory.CloneBitmap(_mainViewModel.Selection.Presenter)
            : _bitmapOperations.ConvertRGB24ToBGRA32(_mainViewModel.Selection.Presenter);

        _pixels1 = new byte[Image1.PixelWidth * Image1.PixelHeight * TRANSITIONS_BPP];
        Image1.CopyPixels(_pixels1, Image1.PixelWidth * TRANSITIONS_BPP, 0);
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
        _transitionHelper.Widening = WideningValue;
        _transitionHelper.Shift = ShiftValue;

        ConfigureTransitionHelperCore();
    }

    private void MarkFinished()
    {
        _transitionHelper.CleanUp();
        _mainViewModel.IsTransitionViewOpen = false;
    }

    private void OK(IView view)
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(ResultImage);
        MarkFinished();
        view.CloseAsync();
    }

    private void Cancel(IView view)
    {
        MarkFinished();
        view.CloseAsync();
    }

    private void UpdateLabelMapImage()
    {
        byte[] mapData = _transitionHelper.GetLabelMap();

        int mapW = ResultImage?.PixelWidth ?? 0;
        int mapH = ResultImage?.PixelHeight ?? 0;

        if (mapData.Length == 0 || mapW == 0 || mapH == 0)
            return;

        var labelBmp = _mediaFactory.CreateEmptyBitmap(mapW, mapH, true);
        labelBmp.WritePixels(
            new PixelRect(0, 0, mapW, mapH),
            mapData,
            mapW * 4);

        LabelMapImage = labelBmp;
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

                byte[] resultPixels = await Task.Run(
                    () => CreateMixedPixels());

                var resImage = _mediaFactory.CreateBitmapFromRaw(
                    width,
                    height,
                    hasAlpha: true,
                    resultPixels,
                    stride: width * 4);

                ResultImage = _mediaFactory.CloneBitmap(resImage);

                OnResultUpdated();
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

    private void SetPropertyTriggerRecalculation<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            _ = TriggerRecalculation();
        }
    }

    private void SetPropertyTriggerRecalculation<T>(
        ref T field,
        T value,
        BricksPipelineRequirements requirements,
        [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            _currentRequirements = requirements;

            if (!string.IsNullOrEmpty(propertyName))
                OnPropertyChanged(propertyName);

            _ = TriggerRecalculation();
        }
    }

    private bool SetCallerPropertyReturn<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            return true;
        }

        return false;
    }
}
