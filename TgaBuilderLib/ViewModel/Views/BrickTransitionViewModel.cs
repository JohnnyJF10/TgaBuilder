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

    private RelayCommand? _pickEdgeColorCommand;
    private RelayCommand<(int X, int Y, int imageNum)>? _mouseOverCommand;

    public IWriteableBitmap? LabelMapImage
    {
        get => _labelMapImage;
        set => SetCallerProperty(ref _labelMapImage, value);
    }

    public int MarkerRadius
    {
        get => _markerRadius;
        set => SetPropertyTriggerRecalculation(ref _markerRadius, value);
    }

    public int ExpectedRegionCount
    {
        get => _expectedRegionCount;
        set => SetPropertyTriggerRecalculation(ref _expectedRegionCount, value);
    }

    public bool ReversePivot
    {
        get => _reversePivot;
        set => SetPropertyTriggerRecalculation(ref _reversePivot, value);
    }

    public bool SliceCornerTiles
    {
        get => _sliceCornerTiles;
        set => SetPropertyTriggerRecalculation(ref _sliceCornerTiles, value);
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    public FilterType SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetCallerPropertyReturn(ref _selectedFilter, value))
            {
                OnPropertyChanged(nameof(SelectedFilterIndex));
                TriggerRecalculation(requiresAnalysis: true);
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
            if (SetCallerPropertyReturn(ref _selectedSegmentationMethod, value))
            {
                OnPropertyChanged(nameof(SelectedSegmentationMethodIndex));
                TriggerRecalculation(requiresAnalysis: true);
            }
        }
    }

    public Color EdgeColor 
    {         
        get => _edgeColor;
        set => SetPropertyTriggerRecalculation(ref _edgeColor, value);
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
        EdgeColor = BitmapOperations.GetPixelBrush(imageNum == 1 ? Image1 : Image2, X, Y);
    }

    private void EndColorPicking()
    {
        IsEyedropperMode = false;

        //_panel.EyedropperEnd();
    }

    protected override bool RequiresFullAnalysisOnPivotChange => false;

    protected override byte[] CreateMixedPixels(bool requiresAnalysis)
    {
        if (requiresAnalysis || TransitionHelper.LastAnalysisMap.Length == 0)
            TransitionHelper.AnalyzeTiles(Pixels1);

        Debug.WriteLine($"Current Pivot: {TransitionHelper.Pivot}, Reverse: {TransitionHelper.ReversePivot}");

        return TransitionHelper.MixSmartTilesPixels(Pixels1, Pixels2);
    }

    protected override void ConfigureTransitionHelperCore()
    {
        TransitionHelper.ReversePivot = ReversePivot;
        TransitionHelper.SliceCornerTiles = SliceCornerTiles;
        TransitionHelper.MarkerRadius = MarkerRadius;
        TransitionHelper.SelectedFilter = SelectedFilter;
        TransitionHelper.SegmentationMethod = SelectedSegmentationMethod;
        TransitionHelper.EdgeColor = EdgeColor;
    }

    protected override void OnResultUpdated()
    {
        UpdateLabelMapImage();
    }

    private void UpdateLabelMapImage()
    {
        byte[] mapData = TransitionHelper.LastAnalysisMap;
        int mapW = TransitionHelper.LastAnalysisWidth;
        int mapH = TransitionHelper.LastAnalysisHeight;

        if (mapData.Length == 0 || mapW == 0 || mapH == 0)
            return;

        var labelBmp = MediaFactory.CreateEmptyBitmap(mapW, mapH, true);
        labelBmp.WritePixels(
            new PixelRect(0, 0, mapW, mapH),
            mapData,
            mapW * 4);

        LabelMapImage = labelBmp;
    }
}

