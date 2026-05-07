using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel;

public class BrickTransitionViewModel : TransitionViewModelBase
{
    public BrickTransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        IBitmapOperations bitmapOperations,
        MainViewModel mainViewModel)
        : base(mediaFactory, transitionHelper, bitmapOperations, mainViewModel)
    {
    }

    private IWriteableBitmap? _labelMapImage;
    private int _markerRadius = 3;
    private int _expectedRegionCount = -1;
    private bool _reversePivot;
    private bool _sliceCornerTiles;
    private bool _isLabelMapExpanded;
    private FilterType _selectedFilter = FilterType.BoxBlur;
    private SegmentationMethod _selectedSegmentationMethod = SegmentationMethod.Watershed;
    private Color _edgeColor = new Color(255, 255, 255, 128);
    private bool _isEyedropperMode;
    private BricksPipelineRequirements _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;

    private RelayCommand? _pickEdgeColorCommand;
    private RelayCommand<(int X, int Y, int imageNum)>? _mouseOverCommand;

    public override TransitionMode TransitionMode
    {
        get => _transitionMode;
        set => SetPropertyTriggerRecalculation(ref _transitionMode, value, BricksPipelineRequirements.RequiresSelectionBuilding, null);
    }

    public override float PivotValue
    {
        get => _pivotValue;
        set => SetPropertyTriggerRecalculationThrottled(ref _pivotValue, value, BricksPipelineRequirements.RequiresSelectionBuilding);
    }


    public IWriteableBitmap? LabelMapImage
    {
        get => _labelMapImage;
        set => SetCallerProperty(ref _labelMapImage, value);
    }

    public int MarkerRadius
    {
        get => _markerRadius;
        set => SetPropertyTriggerRecalculation(ref _markerRadius, value, BricksPipelineRequirements.RequiresAnalysis, null);
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
        set => SetPropertyTriggerRecalculation(ref _selectedSegmentationMethod, value, BricksPipelineRequirements.RequiresAnalysis, null);
    }

    public Color EdgeColor 
    {         
        get => _edgeColor;
        set => SetPropertyTriggerRecalculation(ref _edgeColor, value, BricksPipelineRequirements.RequiresEdgeColoring, null);
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

    public ICommand PickEdgeColorCommand => _pickEdgeColorCommand
        ??= new RelayCommand(PickEdgeColor);
    public ICommand MouseOverCommand => _mouseOverCommand
        ??= new RelayCommand<(int X, int Y, int imageNum)>(MouseOverImages);

    private void MouseOverImages((int X, int Y, int imageNum) MouseOverImagesArgs)
    {
        if (IsEyedropperMode)
        {
            DoColorPicking(MouseOverImagesArgs.X, MouseOverImagesArgs.Y, MouseOverImagesArgs.imageNum);
        }
    }

    public event EventHandler? EyedroppingRequested;



    private void PickEdgeColor() => StartColorPicking();

    private void StartColorPicking()
    {
        IsEyedropperMode = true;

        EyedroppingRequested?.Invoke(this, EventArgs.Empty);
    }

    private void DoColorPicking(int X, int Y, int imageNum)
    {
        EdgeColor = _bitmapOperations.GetPixelBrush(imageNum == 1 ? Image1 : Image2, X, Y);
    }

    protected override byte[] CreateMixedPixels()
    {
        Debug.WriteLine($"Current Pivot: {_transitionHelper.Pivot}, Reverse: {_transitionHelper.ReversePivot}");

        return _transitionHelper.MixBricks(Pixels1, Pixels2);
    }

    protected override void ConfigureTransitionHelperCore()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = _currentRequirements;
        _transitionHelper.ReversePivot = ReversePivot;
        _transitionHelper.SliceCornerTiles = SliceCornerTiles;
        _transitionHelper.MarkerRadius = MarkerRadius;
        _transitionHelper.SelectedFilter = SelectedFilter;
        _transitionHelper.SegmentationMethod = SelectedSegmentationMethod;
        _transitionHelper.EdgeColor = EdgeColor;
    }

    protected override void OnResultUpdated()
    {
        UpdateLabelMapImage();
    }

    protected override void SwapImages()
    {
        _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;
        base.SwapImages();
    }

    protected override void Mix()
    {
        _currentRequirements = BricksPipelineRequirements.RequiresAnalysis;
        base.Mix();
    }

    private void UpdateLabelMapImage()
    {
        byte[] mapData = _transitionHelper.LastAnalysisMap;
        int mapW = _transitionHelper.LastAnalysisWidth;
        int mapH = _transitionHelper.LastAnalysisHeight;

        if (mapData.Length == 0 || mapW == 0 || mapH == 0)
            return;

        var labelBmp = _mediaFactory.CreateEmptyBitmap(mapW, mapH, true);
        labelBmp.WritePixels(
            new PixelRect(0, 0, mapW, mapH),
            mapData,
            mapW * 4);

        LabelMapImage = labelBmp;
    }

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

    /// <summary>
    /// Throttled variant for float properties that carry a pipeline requirement (e.g.
    /// <see cref="PivotValue"/>). Updates <paramref name="field"/>, records the
    /// <paramref name="requirements"/> level, then schedules a single recalculation per
    /// 50 ms window so that rapid slider movements do not flood the brick pipeline.
    /// </summary>
    protected void SetPropertyTriggerRecalculationThrottled(
        ref float field,
        float value,
        BricksPipelineRequirements requirements,
        [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
            return;

        field = value;
        _currentRequirements = requirements;
        OnPropertyChanged(propertyName ?? string.Empty);
        SchedulePivotRecalculation();
    }
}

