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

namespace TgaBuilderLib.ViewModel;

public class TransitionsPresentersViewModel : ThrottledViewModelBase
{
    public TransitionsPresentersViewModel(
    IMediaFactory mediaFactory,
    ITransitionHelper transitionHelper,
    IBitmapOperations bitmapOperations)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _bitmapOperations = bitmapOperations;

        _image1 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _image2 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _resultImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);

        _pixels1 = new byte[64 * 64 * 4];
        _pixels2 = new byte[64 * 64 * 4];
    }

    private const int TRANSITIONS_BPP = 4;

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly IBitmapOperations _bitmapOperations;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private IWriteableBitmap _image1;
    private IWriteableBitmap _image2;
    private IWriteableBitmap _resultImage;
    private byte[] _pixels1;
    private byte[] _pixels2;

    private IWriteableBitmap? _labelMapImage;
    private IWriteableBitmap? _indicatorMapImage;

    private bool _initTextVisible = true;
    private bool _isLabelMapExpanded;
    private bool _isIndicatorMapVisible;

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


    public BricksPipelineRequirements CurrentRequirements { get; set; } 
        = BricksPipelineRequirements.RequiresAnalysis;

    // =====================================================================
    // Mode selection (TransitionType enum)
    // =====================================================================

    private TransitionType _selectedTransitionType = TransitionType.Bricks;

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
                    CurrentRequirements = BricksPipelineRequirements.RequiresAnalysis;

                if (_selectedTransitionType == TransitionType.Smooth)
                {
                    IsLabelMapExpanded = false;
                    IsExplicitTileVisibilityEraseMode = false;
                    IsExplicitTileVisibilityDrawMode = false;
                    IsEyedropperMode = false;
                    IsShadowEyedropperMode = false;
                }

                _ = TriggerRecalculation();
            }
        }
    }

    // Computed convenience helpers (used by visibility bindings in the views)
    public bool IsSmoothMode => _selectedTransitionType == TransitionType.Smooth;
    public bool IsBrickMode => _selectedTransitionType == TransitionType.Bricks;

    // =====================================================================
    // User Mouse Interaction
    // =====================================================================

    private bool _isEyedropperMode;
    private bool _isShadowEyedropperMode;
    private bool _isExplicitTileVisibilityDrawMode;
    private bool _isExplicitTileVisibilityEraseMode;
    public bool IsEyedropperMode
    {
        get => _isEyedropperMode;
        set
        {
            if (_isEyedropperMode == value)
                return;

            _isEyedropperMode = value;
            OnPropertyChanged(nameof(IsEyedropperMode));

            if (!value)
                return;

            _isShadowEyedropperMode = false;
            OnPropertyChanged(nameof(IsShadowEyedropperMode));
        }
    }

    public bool IsShadowEyedropperMode
    {
        get => _isShadowEyedropperMode;
        set
        {
            if (_isShadowEyedropperMode == value)
                return;

            _isShadowEyedropperMode = value;
            OnPropertyChanged(nameof(IsShadowEyedropperMode));

            if (!value)
                return;

            _isEyedropperMode = false;
            OnPropertyChanged(nameof(IsEyedropperMode));
        }
    }

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
    // Colors
    // =====================================================================
    private Color _edgeColor = new Color(255, 255, 255, 128);
    private Color _shadowColor = new Color(42, 42, 42, 42);


    public Color EdgeColor
    {
        get => _edgeColor;
        set => SetPropertyTriggerRecalculation(ref _edgeColor, value);
    }

    public Color ShadowColor
    {
        get => _shadowColor;
        set => SetPropertyTriggerRecalculation(ref _shadowColor, value);
    }


    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand? _swapImagesCommand;
    private RelayCommand<(int, int)>? _setExplicitTileVisibilityCommand;
    private RelayCommand? _resetExplicitVisibilityCommand;
    private RelayCommand<(int, int)>? _requestLabelIndicatorCommand;
    private RelayCommand<(int X, int Y, int imageNum)>? _mouseOverCommand;


    public ICommand SwapImagesCommand => _swapImagesCommand ??= new RelayCommand(SwapImages);
    public ICommand SetExplicitTileVisibilityCommand => _setExplicitTileVisibilityCommand
        ??= new RelayCommand<(int, int)>(args =>
        SetExplicitTileVisibility(args.Item1, args.Item2));
    public ICommand ResetExplicitVisibilityCommand => _resetExplicitVisibilityCommand
        ??= new RelayCommand(ResetAllExplicitTileVisibility);
    public ICommand RequestLabelIndicatorCommand => _requestLabelIndicatorCommand
        ??= new RelayCommand<(int, int)>(args => RequestNewIndicatorMapImage(args.Item1, args.Item2));
    public ICommand MouseOverCommand => _mouseOverCommand
    ??= new RelayCommand<(int X, int Y, int imageNum)>(MouseOverImages);


    private void MouseOverImages((int X, int Y, int imageNum) args)
    {
        if (IsEyedropperMode || IsShadowEyedropperMode)
            DoColorPicking(args.X, args.Y, args.imageNum, IsShadowEyedropperMode);
    }

    private void DoColorPicking(int x, int y, int imageNum, bool pickShadowColor)
    {
        var sampledColor = _bitmapOperations.GetPixelBrush(imageNum == 1 ? Image1 : Image2, x, y);
        if (pickShadowColor)
            ShadowColor = sampledColor;
        else
            EdgeColor = sampledColor;
    }

    private void OnResultUpdated()
    {
        if (IsBrickMode)
            UpdateLabelMapImage();
    }

    private void SwapImages()
    {
        if (IsBrickMode)
            CurrentRequirements = BricksPipelineRequirements.RequiresAnalysis;

        var tempImage = Image1;
        Image1 = Image2;
        Image2 = tempImage;

        var tempPixels = _pixels1;
        _pixels1 = _pixels2;
        _pixels2 = tempPixels;

        _ = TriggerRecalculation();
    }

    public void LoadImage1(IWriteableBitmap bitmap)
    {
        Image1 = bitmap.HasAlpha
            ? _mediaFactory.CloneBitmap(bitmap)
            : _bitmapOperations.ConvertRGB24ToBGRA32(bitmap);

        _pixels1 = new byte[Image1.PixelWidth * Image1.PixelHeight * TRANSITIONS_BPP];
        Image1.CopyPixels(_pixels1, Image1.PixelWidth * TRANSITIONS_BPP, 0);
        InitTextVisible = false;

        _transitionHelper.CleanUp();
    }

    public void LoadImage2(IWriteableBitmap bitmap)
    {
        Image2 = bitmap.HasAlpha
            ? _mediaFactory.CloneBitmap(bitmap)
            : _bitmapOperations.ConvertRGB24ToBGRA32(bitmap);

        _pixels2 = new byte[Image2.PixelWidth * Image2.PixelHeight * TRANSITIONS_BPP];
        Image2.CopyPixels(_pixels2, Image2.PixelWidth * TRANSITIONS_BPP, 0);
        InitTextVisible = false;

        _transitionHelper.CleanUp();
    }

    private bool CompareInputSpecs()
        => Image1.PixelWidth == Image2.PixelWidth &&
           Image1.PixelHeight == Image2.PixelHeight &&
           Image1.HasAlpha == Image2.HasAlpha;

    public void Mix()
    {
        if (IsBrickMode)
            CurrentRequirements = BricksPipelineRequirements.RequiresAnalysis;

        if (!CompareInputSpecs())
            return;

        _transitionHelper.Width = Image1.PixelWidth;
        _transitionHelper.Height = Image1.PixelHeight;

        var resultPixels = CreateMixedPixels();

        ResultImage = _mediaFactory.CreateEmptyBitmap(Image1.PixelWidth, Image1.PixelHeight, Image1.HasAlpha);
        ResultImage.WritePixels(
            new PixelRect(0, 0, ResultImage.PixelWidth, ResultImage.PixelHeight),
            resultPixels,
            ResultImage.PixelWidth * TRANSITIONS_BPP);

        OnResultUpdated();
    }

    private byte[] CreateMixedPixels()
    => IsSmoothMode
        ? _transitionHelper.MixSmooth(Pixels1, Pixels2)
        : _transitionHelper.MixBricks(Pixels1, Pixels2);

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

        bool visibilityChnaged = _transitionHelper.SetExplicitTileVisibility(label, IsExplicitTileVisibilityDrawMode);

        CurrentRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;

        if (!visibilityChnaged)
            return;

        _ = TriggerRecalculation();
        Debug.WriteLine("Explicit tile visibility changed for label " + label);
    }

    private void ResetAllExplicitTileVisibility()
    {
        _transitionHelper.ResetAllExplicitTileVisibility();

        CurrentRequirements
            = BricksPipelineRequirements.RequiresSelectionBuilding;

        _ = TriggerRecalculation();
    }

    public bool DoPreProcessing()
    {
        if (!CompareInputSpecs())
            return false;

        int width = Image1.PixelWidth;
        int height = Image1.PixelHeight;

        _transitionHelper.EdgeColor = EdgeColor;
        _transitionHelper.ShadowColor = ShadowColor;
        _transitionHelper.CurrentBricksPipelineRequirements = CurrentRequirements;

        return true;
    }

    public async Task DoRecalculation()
    {

        byte[] resultPixels = await Task.Run(
            () => CreateMixedPixels());

        var resImage = _mediaFactory.CreateBitmapFromRaw(
            Image1.PixelWidth,
            Image1.PixelHeight,
            hasAlpha: true,
            resultPixels,
            stride: Image1.PixelWidth * 4);
        ResultImage = _mediaFactory.CloneBitmap(resImage);

        OnResultUpdated();
    }

    protected override bool PreProcess() => DoPreProcessing();

    protected override void Recalculate() => _ = DoRecalculation();
}
