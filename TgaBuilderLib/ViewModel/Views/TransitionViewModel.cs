using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel;

public class TransitionViewModel : TransitionViewModelBase
{
    public TransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        IBitmapOperations bitmapOperations,
        MainViewModel mainViewModel)
        : base(mediaFactory, transitionHelper, bitmapOperations, mainViewModel)
    {
    }

    // =====================================================================
    // Mode selection
    // =====================================================================

    private bool _isSmoothMode = true;

    public bool IsSmoothMode
    {
        get => _isSmoothMode;
        set
        {
            if (_isSmoothMode != value)
            {
                _isSmoothMode = value;
                OnPropertyChanged(nameof(IsSmoothMode));
                OnPropertyChanged(nameof(IsBrickMode));

                if (!_isSmoothMode)
                    _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;

                _ = TriggerRecalculation();
            }
        }
    }

    public bool IsBrickMode
    {
        get => !_isSmoothMode;
        set
        {
            if (value != !_isSmoothMode)
                IsSmoothMode = !value;
        }
    }

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

    public override TransitionMode SelectedTransitionMode
    {
        get => _selectedtransitionMode;
        set => SetPropertyTriggerRecalculation(ref _selectedtransitionMode, value, BricksPipelineRequirements.RequiresSelectionBuilding);
    }

    public override float PivotValue
    {
        get => _pivotValue;
        set => SetPropertyTriggerRecalculation(ref _pivotValue, value, BricksPipelineRequirements.RequiresSelectionBuilding);
    }

    public override float WideningValue
    {
        get => _wideningValue;
        set => SetPropertyTriggerRecalculation(ref _wideningValue, value, BricksPipelineRequirements.RequiresSelectionBuilding);
    }

    public override float ShiftValue
    {
        get => _shiftValue;
        set => SetPropertyTriggerRecalculation(ref _shiftValue, value, BricksPipelineRequirements.RequiresSelectionBuilding);
    }

    public IWriteableBitmap? LabelMapImage
    {
        get => _labelMapImage;
        set => SetCallerProperty(ref _labelMapImage, value);
    }

    public bool InvertGrayscale
    {
        get => _invertGrayscale;
        set => SetPropertyTriggerRecalculation(ref _invertGrayscale, value, BricksPipelineRequirements.RequiresAnalysis, null);
    }

    public int MarkerRadius
    {
        get => _markerRadius;
        set => SetPropertyTriggerRecalculation(ref _markerRadius, value, BricksPipelineRequirements.RequiresAnalysis);
    }

    public bool ReversePivot
    {
        get => _reversePivot;
        set => SetPropertyTriggerRecalculation(ref _reversePivot, value, BricksPipelineRequirements.RequiresSelectionBuilding, null);
    }

    public bool SliceCornerTiles
    {
        get => _sliceCornerTiles;
        set => SetPropertyTriggerRecalculation(ref _sliceCornerTiles, value, BricksPipelineRequirements.RequiresSelectionBuilding, null);
    }

    public bool ProtectEdges
    {
        get => _protectEdges;
        set => SetPropertyTriggerRecalculation(ref _protectEdges, value, BricksPipelineRequirements.RequiresSelectionBuilding, null);
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    public FilterType SelectedFilter
    {
        get => _selectedFilter;
        set => SetPropertyTriggerRecalculation(ref _selectedFilter, value, BricksPipelineRequirements.RequiresAnalysis, null);
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
        set => SetPropertyTriggerRecalculation(ref _felzenszwalbMinSize, value, BricksPipelineRequirements.RequiresAnalysis);
    }

    public float FelzenszwalbScale
    {
        get => _felzenszwalbScale;
        set => SetPropertyTriggerRecalculation(ref _felzenszwalbScale, value, BricksPipelineRequirements.RequiresAnalysis);
    }

    public int SlicSegmentCount
    {
        get => _slicSegmentCount;
        set => SetPropertyTriggerRecalculation(ref _slicSegmentCount, value, BricksPipelineRequirements.RequiresAnalysis);
    }

    public float SlicCompactness
    {
        get => _slicCompactness;
        set => SetPropertyTriggerRecalculation(ref _slicCompactness, value, BricksPipelineRequirements.RequiresAnalysis);
    }

    public int QuickshiftMaxDist
    {
        get => _quickshiftMaxDist;
        set => SetPropertyTriggerRecalculation(ref _quickshiftMaxDist, value, BricksPipelineRequirements.RequiresAnalysis);
    }

    public float QuickshiftRatio
    {
        get => _quickshiftRatio;
        set => SetPropertyTriggerRecalculation(ref _quickshiftRatio, value, BricksPipelineRequirements.RequiresAnalysis);
    }

    public Color EdgeColor
    {
        get => _edgeColor;
        set => SetPropertyTriggerRecalculation(ref _edgeColor, value, BricksPipelineRequirements.RequiresEdgeColoring);
    }

    public EdgeBlendMode BlendMode
    {
        get => _blendMode;
        set => SetPropertyTriggerRecalculation(ref _blendMode, value, BricksPipelineRequirements.RequiresEdgeColoring);
    }

    public Array EdgeBlendModes => Enum.GetValues(typeof(EdgeBlendMode));

    public int EdgeWidth
    {
        get => _edgeWidth;
        set => SetPropertyTriggerRecalculation(ref _edgeWidth, value, BricksPipelineRequirements.RequiresEdgeColoring);
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
    // Core pipeline overrides
    // =====================================================================

    protected override byte[] CreateMixedPixels()
        => _isSmoothMode
            ? _transitionHelper.MixSmooth(Pixels1, Pixels2)
            : _transitionHelper.MixBricks(Pixels1, Pixels2);

    protected override void ConfigureTransitionHelperCore()
    {
        if (_isSmoothMode)
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

    protected override void OnResultUpdated()
    {
        if (!_isSmoothMode)
            UpdateLabelMapImage();
    }

    protected override void SwapImages()
    {
        if (!_isSmoothMode)
            _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;
        base.SwapImages();
    }

    protected override void Mix()
    {
        if (!_isSmoothMode)
            _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;
        base.Mix();
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
    // Brick-specific SetPropertyTriggerRecalculation override
    // =====================================================================

    protected void SetPropertyTriggerRecalculation<T>(
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
}
