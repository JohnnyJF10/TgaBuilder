using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;

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

    private const int MAX_SIZE = 512;
    private const int MIN_SIZE = 8;

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

        var tempPixels = _transitionHelper.Pixels1;
         _transitionHelper.Pixels1 = _transitionHelper.Pixels2;
         _transitionHelper.Pixels2 = tempPixels;

        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

        _ = TriggerRecalculation();
    }

    public void LoadImage1(IWriteableBitmap bitmap)
    {
        Image1 = PrepareAndResizeImage(bitmap);
        _transitionHelper.Pixels1 = ExtractPixels(Image1);
    
        InitTextVisible = false;
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;
    
        UpdateHelperDimensionsAndResult(Image1);
    
        if (AreDimensionsDifferent(Image1, Image2))
        {
            Image2 = ImageInResize(Image2, Image1.PixelWidth, Image1.PixelHeight);
           _transitionHelper.Pixels2 = ExtractPixels(Image2);
        }
    
        _ = TriggerRecalculation();
    }
    
    public void LoadImage2(IWriteableBitmap bitmap)
    {
        Image2 = PrepareAndResizeImage(bitmap);
        _transitionHelper.Pixels2 = ExtractPixels(Image2);
    
        InitTextVisible = false;
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresDrawing;
    
        UpdateHelperDimensionsAndResult(Image2);
    
        if (AreDimensionsDifferent(Image1, Image2))
        {
            Image1 = ImageInResize(Image1, Image2.PixelWidth, Image2.PixelHeight);
            _transitionHelper.Pixels1 = ExtractPixels(Image1);
            _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;
        }

        _ = TriggerRecalculation();
    }
    
    private IWriteableBitmap PrepareAndResizeImage(IWriteableBitmap bitmap)
    {
        var imageIn = bitmap.HasAlpha
            ? _mediaFactory.CloneBitmap(bitmap)
            : _bitmapOperations.ConvertRGB24ToBGRA32(bitmap);
    
        return ImageInResize(imageIn);
    }
    
    private byte[] ExtractPixels(IWriteableBitmap image)
    {
        // Todo: Let helper prepare buffers instead of copying pixels here
        var pixels = new byte[image.PixelWidth * image.PixelHeight * TRANSITIONS_BPP];
        image.CopyPixels(pixels, image.PixelWidth * TRANSITIONS_BPP, 0);
        return pixels;
    }
    
    private void UpdateHelperDimensionsAndResult(IWriteableBitmap referenceImage)
    {
        _transitionHelper.Width = referenceImage.PixelWidth;
        _transitionHelper.Height = referenceImage.PixelHeight;
    
        TransitionOutVM.ResultImage = _mediaFactory.CreateEmptyBitmap(
            referenceImage.PixelWidth, 
            referenceImage.PixelHeight, 
            true);
    }
    
    private bool AreDimensionsDifferent(IWriteableBitmap img1, IWriteableBitmap img2)
    {        
        return img1.PixelWidth != img2.PixelWidth || 
               img1.PixelHeight != img2.PixelHeight;
    }

    public IWriteableBitmap ImageInResize(IWriteableBitmap imIn, int newWidth = -1, int newHeight = -1)
    {
        int acceptedWidth = newWidth > 0 ? newWidth : Math.Clamp(imIn.PixelWidth, MIN_SIZE, MAX_SIZE);
        int acceptedHeight = newHeight > 0 ? newHeight : Math.Clamp(imIn.PixelHeight, MIN_SIZE, MAX_SIZE);

        if (acceptedWidth == imIn.PixelWidth && acceptedHeight == imIn.PixelHeight)
            return imIn;

        return _bitmapOperations.ResizeScaled(imIn, acceptedWidth, acceptedHeight);
    }

    public void Mix()
    {
        if (_transitionHelper.TypeOfTransition == TransitionType.Bricks)
            _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

        _transitionHelper.Mix();
    }



    public void ResetImages()
    {
        InitTextVisible = true;
        _image1 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _image2 = _mediaFactory.CreateEmptyBitmap(64, 64, true);

        EdgeColor = new Color(255, 255, 255, 128);
        ShadowColor = new Color(42, 42, 42, 42);
    }

    public void EndMouseInteractivity()
    {
        IsEyedropperMode = false;
        IsShadowEyedropperMode = false;
    }

    private void ConfigureTransitionHelper()
    {
        _transitionHelper.EdgeColor = EdgeColor;
        _transitionHelper.ShadowColor = ShadowColor;        
    }

    protected override async Task TriggerRecalculation()
    {
        await _transitionHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}