using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;
using Transitions;

namespace TgaBuilderLib.ViewModel;

public class TransitionInViewModel : ThrottledViewModelBase
{
    public TransitionInViewModel(
    IMediaFactory mediaFactory,
    ITransitionHelper transitionHelper,
    IBitmapOperations bitmapOperations,
    TransitionOutViewModel transitionOutViewModel
    )
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _bitmapOperations = bitmapOperations;
        TransitionOutVM = transitionOutViewModel;

        _image1 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _image2 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
    }

    private const int TRANSITIONS_BPP = 4;

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly IBitmapOperations _bitmapOperations;

    public readonly TransitionOutViewModel TransitionOutVM;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private IWriteableBitmap _image1;
    private IWriteableBitmap _image2;


    private bool _initTextVisible = true;

    public IVisualInvalidator? VisualInvalidator { get; set; }


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


    public bool InitTextVisible
    {
        get => _initTextVisible;
        set => SetCallerProperty(ref _initTextVisible, value);
    }



    // =====================================================================
    // User Mouse Interaction
    // =====================================================================

    private bool _isEyedropperMode;
    private bool _isShadowEyedropperMode;
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
    private RelayCommand<(int X, int Y, int imageNum)>? _mouseOverCommand;


    public ICommand SwapImagesCommand => _swapImagesCommand ??= new RelayCommand(SwapImages);
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


    private void SwapImages()
    {
        if (_transitionHelper.TypeOfTransition == TransitionType.Bricks)
            _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

        var tempImage = Image1;
        Image1 = Image2;
        Image2 = tempImage;

        var tempPixels = _transitionHelper.pixels1;
         _transitionHelper.pixels1 = _transitionHelper.pixels2;
         _transitionHelper.pixels2 = tempPixels;

        _ = TriggerRecalculation();
    }

    public void LoadImage1(IWriteableBitmap bitmap)
    {
        Image1 = bitmap.HasAlpha
            ? _mediaFactory.CloneBitmap(bitmap)
            : _bitmapOperations.ConvertRGB24ToBGRA32(bitmap);

        _transitionHelper.pixels1 = new byte[Image1.PixelWidth * Image1.PixelHeight * TRANSITIONS_BPP];
        Image1.CopyPixels(_transitionHelper.pixels1, Image1.PixelWidth * TRANSITIONS_BPP, 0);
        InitTextVisible = false;
    }

    public void LoadImage2(IWriteableBitmap bitmap)
    {
        Image2 = bitmap.HasAlpha
            ? _mediaFactory.CloneBitmap(bitmap)
            : _bitmapOperations.ConvertRGB24ToBGRA32(bitmap);

        _transitionHelper.pixels2 = new byte[Image2.PixelWidth * Image2.PixelHeight * TRANSITIONS_BPP];
        Image2.CopyPixels(_transitionHelper.pixels2, Image2.PixelWidth * TRANSITIONS_BPP, 0);
        InitTextVisible = false;
    }

    private bool CompareInputSpecs()
        => Image1.PixelWidth == Image2.PixelWidth &&
           Image1.PixelHeight == Image2.PixelHeight &&
           Image1.HasAlpha == Image2.HasAlpha;

    public void Mix()
    {
        if (_transitionHelper.TypeOfTransition == TransitionType.Bricks)
            _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

        if (!CompareInputSpecs())
            return;

        _transitionHelper.Width = Image1.PixelWidth;
        _transitionHelper.Height = Image1.PixelHeight;

        var resultPixels = _transitionHelper.Mix();

        TransitionOutVM.ResultImage = _mediaFactory.CreateEmptyBitmap(Image1.PixelWidth, Image1.PixelHeight, Image1.HasAlpha);
        TransitionOutVM.ResultImage.WritePixels(
            new PixelRect(0, 0, TransitionOutVM.ResultImage.PixelWidth, TransitionOutVM.ResultImage.PixelHeight),
            resultPixels,
            TransitionOutVM.ResultImage.PixelWidth * TRANSITIONS_BPP);

        TransitionOutVM.OnResultUpdated();
    }



    public void ResetImages()
    {
        _image1 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _image2 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
    }

    public void ResetBools()
    {
        InitTextVisible = true;
        IsEyedropperMode = false;
        IsShadowEyedropperMode = false;
    }

    public bool DoPreProcessing()
    {
        if (!CompareInputSpecs())
            return false;

        _transitionHelper.Width = Image1.PixelWidth;
        _transitionHelper.Height = Image1.PixelHeight;

        _transitionHelper.EdgeColor = EdgeColor;
        _transitionHelper.ShadowColor = ShadowColor;

        return true;
    }

    public async Task DoRecalculation()
    {

        byte[] resultPixels = await Task.Run(
            () => _transitionHelper.Mix());

        var resImage = _mediaFactory.CreateBitmapFromRaw(
            Image1.PixelWidth,
            Image1.PixelHeight,
            hasAlpha: true,
            resultPixels,
            stride: Image1.PixelWidth * 4);
        TransitionOutVM.ResultImage = _mediaFactory.CloneBitmap(resImage);

        TransitionOutVM.OnResultUpdated();
    }

    protected override bool PreProcess() => DoPreProcessing();

    protected override async Task Recalculate() => await DoRecalculation();
}
