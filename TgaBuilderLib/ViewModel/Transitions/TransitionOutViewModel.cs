using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;
using Transitions;

namespace TgaBuilderLib.ViewModel;

public class TransitionOutViewModel : ThrottledViewModelBase
{
    public TransitionOutViewModel(
    IMediaFactory mediaFactory,
    ITransitionHelper transitionHelper,
    IBitmapOperations bitmapOperations)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _bitmapOperations = bitmapOperations;

        _resultImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);
    }

    private const int TRANSITIONS_BPP = 4;

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly IBitmapOperations _bitmapOperations;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private IWriteableBitmap _resultImage;

    private IWriteableBitmap? _labelMapImage;
    private IWriteableBitmap? _indicatorMapImage;

    private bool _isLabelMapExpanded;
    private bool _isIndicatorMapVisible;

    private bool _initTextVisible = true;

    public IVisualInvalidator? VisualInvalidator { get; set; }



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

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    public IWriteableBitmap? LabelMapImage
    {
        get => _labelMapImage;
        set => SetCallerProperty(ref _labelMapImage, value);
    }

    public IWriteableBitmap? IndicatorMapImage
    {
        get => _indicatorMapImage;
        set => SetCallerProperty(ref _indicatorMapImage, value);
    }

    public bool IsIndicatorMapVisible
    {
        get => _isIndicatorMapVisible;
        set => SetCallerProperty(ref _isIndicatorMapVisible, value);
    }




    // =====================================================================
    // User Mouse Interaction
    // =====================================================================


    private bool _isExplicitTileVisibilityDrawMode;
    private bool _isExplicitTileVisibilityEraseMode;


    public bool IsExplicitTileVisibilityDrawMode
    {
        get => _isExplicitTileVisibilityDrawMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityDrawMode, value, nameof(IsExplicitTileVisibilityDrawMode));

            if (!value)
                return;

            _isExplicitTileVisibilityEraseMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityEraseMode));
        }
    }

    public bool IsExplicitTileVisibilityEraseMode
    {
        get => _isExplicitTileVisibilityEraseMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityEraseMode, value, nameof(IsExplicitTileVisibilityEraseMode));
            if (!value)
                return;

            _isExplicitTileVisibilityDrawMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityDrawMode));
        }
    }


    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand<(int, int)>? _setExplicitTileVisibilityCommand;
    private RelayCommand? _resetExplicitVisibilityCommand;
    private RelayCommand<(int, int)>? _requestLabelIndicatorCommand;


    public ICommand SetExplicitTileVisibilityCommand => _setExplicitTileVisibilityCommand
        ??= new RelayCommand<(int, int)>(args =>
        SetExplicitTileVisibility(args.Item1, args.Item2));
    public ICommand ResetExplicitVisibilityCommand => _resetExplicitVisibilityCommand
        ??= new RelayCommand(ResetAllExplicitTileVisibility);
    public ICommand RequestLabelIndicatorCommand => _requestLabelIndicatorCommand
        ??= new RelayCommand<(int, int)>(args => RequestNewIndicatorMapImage(args.Item1, args.Item2));




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
        if (!IsExplicitTileVisibilityEraseMode && !IsExplicitTileVisibilityDrawMode)
            return;

        int label = _transitionHelper.GetLabelAtPixel(x, y);

        if (label == 0)
            return;

        bool visibilityChanged = _transitionHelper.SetExplicitTileVisibility(label, IsExplicitTileVisibilityDrawMode);

        _transitionHelper.CurrentBricksPipelineRequirements 
        = BricksPipelineRequirements.RequiresSelectionBuilding;

        if (!visibilityChanged)
            return;

        _ = TriggerRecalculation();
    }

    private void ResetAllExplicitTileVisibility()
    {
        _transitionHelper.ResetAllExplicitTileVisibility();

        _transitionHelper.CurrentBricksPipelineRequirements
            = BricksPipelineRequirements.RequiresSelectionBuilding;

        _ = TriggerRecalculation();
    }

    public void ResetImages()
    {
        _resultImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _labelMapImage = null;
    }

    public void ResetBools()
    {
        InitTextVisible = true;
        IsLabelMapExpanded = false;
        IsExplicitTileVisibilityDrawMode = false;
        IsExplicitTileVisibilityEraseMode = false;
    }

    public bool DoPreProcessing()
    {


        return true;
    }

    public async Task DoRecalculation()
    {

        byte[] resultPixels = await Task.Run(
            () => _transitionHelper.Mix());

        var resImage = _mediaFactory.CreateBitmapFromRaw(
            _transitionHelper.Width,
            _transitionHelper.Height,
            hasAlpha: true,
            resultPixels,
            stride: _transitionHelper.Width * 4);
        ResultImage = _mediaFactory.CloneBitmap(resImage);

        OnResultUpdated();
    }

    public void OnResultUpdated()
    {
        if (_transitionHelper.TypeOfTransition == TransitionType.Bricks)
            UpdateLabelMapImage();
    }

    protected override bool PreProcess() => DoPreProcessing();

    protected override async Task Recalculate() => await DoRecalculation();
}
