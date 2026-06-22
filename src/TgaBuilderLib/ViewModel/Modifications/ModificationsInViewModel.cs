using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel;

public class ModificationsInViewModel : ThrottledViewModelBase
{
    public ModificationsInViewModel(
    IMediaFactory mediaFactory,
    IModificationsHelper modificationHelper,
    IBitmapOperations bitmapOperations,
    ModificationsOutViewModel modificationOutViewModel
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

    public readonly ModificationsOutViewModel ModificationOutVM;

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

        // Provision the reusable buffer set for the new input size, then copy the
        // input pixels into the helper's buffer.
        _modificationHelper.EnsureBuffers(ImageIn.PixelWidth, ImageIn.PixelHeight);
        _modificationHelper.PixelsInput = CopyPixelsInto(ImageIn, _modificationHelper.PixelsInput);

        ModificationOutVM.ImageOut = _mediaFactory.CreateEmptyBitmap(
            ImageIn.PixelWidth,
            ImageIn.PixelHeight,
            true);

        _ = TriggerRecalculation();
    }

    private IWriteableBitmap PrepareAndResizeImage(IWriteableBitmap bitmap)
    {
        var imageIn = bitmap.HasAlpha
            ? _mediaFactory.CloneBitmap(bitmap)
            : _bitmapOperations.ConvertRGB24ToBGRA32(bitmap);

        return ImageInResize(imageIn);
    }

    // Copies the image pixels into the helper's reusable buffer. Once EnsureBuffers
    // has run for the current size the target is correctly sized and reused in place;
    // the fresh-array fallback only guards against an unexpected size mismatch.
    private byte[] CopyPixelsInto(IWriteableBitmap image, byte[] target)
    {
        int stride = image.PixelWidth * TRANSITIONS_BPP;
        int needed = stride * image.PixelHeight;

        if (target.Length != needed)
            target = new byte[needed];

        image.CopyPixels(target, stride, 0);
        return target;
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