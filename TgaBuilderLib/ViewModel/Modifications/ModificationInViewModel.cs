using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel;

public class ModificationInViewModel : ThrottledViewModelBase
{
    public ModificationInViewModel(
    IMediaFactory mediaFactory,
    IModificationsHelper modificationHelper,
    IBitmapOperations bitmapOperations,
    ModificationOutViewModel modificationOutViewModel
    )
    {
        _mediaFactory = mediaFactory;
        _modificationHelper = modificationHelper;
        _bitmapOperations = bitmapOperations;
        ModificationOutVM = modificationOutViewModel;

        _imageIn = _mediaFactory.CreateEmptyBitmap(64, 64, true);
    }

    private const int TRANSITIONS_BPP = 4;

    private const int MAX_SIZE = 4096;
    private const int MIN_SIZE = 8;

    private readonly IMediaFactory _mediaFactory;
    private readonly IModificationsHelper _modificationHelper;
    private readonly IBitmapOperations _bitmapOperations;

    public readonly ModificationOutViewModel ModificationOutVM;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private IWriteableBitmap _imageIn;




    public IWriteableBitmap ImageIn
    {
        get => _imageIn;
        set => SetCallerProperty(ref _imageIn, value);
    }


    // =====================================================================
    // User Mouse Interaction
    // =====================================================================

    private bool _isEyedropperMode;
    public bool IsEyedropperMode
    {
        get => _isEyedropperMode;
        set
        {
            if (_isEyedropperMode == value)
                return;

            _isEyedropperMode = value;
            OnPropertyChanged(nameof(IsEyedropperMode));
        }
    }

    // =====================================================================
    // Colors
    // =====================================================================
    private Color _overlayColor = new Color(0, 0, 0, 0);


    public Color OverlayColor
    {
        get => _overlayColor;
        set => SetPropertyTriggerRecalculation(ref _overlayColor, value);
    }


    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand<(int X, int Y)>? _mouseOverCommand;

    public ICommand MouseOverCommand => _mouseOverCommand
    ??= new RelayCommand<(int X, int Y)>(MouseOverImages);


    private void MouseOverImages((int X, int Y) pos)
    {
        if (IsEyedropperMode)
            DoColorPicking(pos.X, pos.Y);
    }

    private void DoColorPicking(int x, int y)
    {
        var sampledColor = _bitmapOperations.GetPixelBrush(ImageIn, x, y);

        OverlayColor = sampledColor;
    }


    public void LoadImageIn(IWriteableBitmap bitmap)
    {
        ImageIn = PrepareAndResizeImage(bitmap);
        _modificationHelper.PixelsInput = ExtractPixels(ImageIn);
    
        UpdateHelperDimensionsAndResult(ImageIn);
    
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
        _modificationHelper.Width = referenceImage.PixelWidth;
        _modificationHelper.Height = referenceImage.PixelHeight;
    
        ModificationOutVM.ImageOut = _mediaFactory.CreateEmptyBitmap(
            referenceImage.PixelWidth, 
            referenceImage.PixelHeight, 
            true);
    }

    public IWriteableBitmap ImageInResize(IWriteableBitmap imIn, int newWidth = -1, int newHeight = -1)
    {
        int acceptedWidth = newWidth > 0 ? newWidth : Math.Clamp(imIn.PixelWidth, MIN_SIZE, MAX_SIZE);
        int acceptedHeight = newHeight > 0 ? newHeight : Math.Clamp(imIn.PixelHeight, MIN_SIZE, MAX_SIZE);

        if (acceptedWidth == imIn.PixelWidth && acceptedHeight == imIn.PixelHeight)
            return imIn;

        return _bitmapOperations.ResizeScaled(imIn, acceptedWidth, acceptedHeight);
    }

    public void Apply()
    {
        _modificationHelper.Apply();
    }



    public void ResetImages()
    {
        _imageIn = _mediaFactory.CreateEmptyBitmap(64, 64, true);

        OverlayColor = new Color(0, 0, 0, 0);
    }

    public void EndMouseInteractivity()
    {
        IsEyedropperMode = false;
    }

    private void ConfigureTransitionHelper()
    {
        _modificationHelper.ColorOverlay = OverlayColor;      
    }

    protected override async Task TriggerRecalculation()
    {
        await _modificationHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}