using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel;

// =========================================================================
// ColorOverrideViewModel
//
// Drives the "Color Override" expander: an optional stage that transfers the
// colour (or overall tone) of a secondary input texture onto the texture
// currently being modified, while keeping the original luminance / detail.
//
// The toggle (IsColorOverrideEnabled) both enables the effect and reveals the
// secondary input image in the window. The secondary texture is kept at its
// own resolution — the helper samples it with normalised coordinates, so it
// works regardless of any size difference with the input texture.
// =========================================================================

public class ColorOverrideViewModel : ThrottledViewModelBase
{
    public ColorOverrideViewModel(
        IMediaFactory mediaFactory,
        IModificationsHelper modificationHelper,
        IBitmapOperations bitmapOperations,
        ModificationInViewModel modificationInVM)
    {
        _mediaFactory = mediaFactory;
        _modificationHelper = modificationHelper;
        _bitmapOperations = bitmapOperations;
        ModificationInVM = modificationInVM;

        _secondaryImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);
    }

    private const int BPP = 4;

    private const int MAX_SIZE = 4096;
    private const int MIN_SIZE = 8;

    private readonly IMediaFactory _mediaFactory;
    private readonly IModificationsHelper _modificationHelper;
    private readonly IBitmapOperations _bitmapOperations;

    public ModificationInViewModel ModificationInVM { get; }

    // =====================================================================
    // Secondary input image
    // =====================================================================

    private IWriteableBitmap _secondaryImage;
    private bool _secondaryInitTextVisible = true;

    public IWriteableBitmap SecondaryImage
    {
        get => _secondaryImage;
        set => SetCallerProperty(ref _secondaryImage, value);
    }

    public bool SecondaryInitTextVisible
    {
        get => _secondaryInitTextVisible;
        set => SetCallerProperty(ref _secondaryInitTextVisible, value);
    }

    // =====================================================================
    // Color override adjustments
    // Enabled:        on/off toggle (also reveals the secondary image)
    // Amount:         0 .. 1   master strength of the whole effect
    // Decolorize:     0 .. 1   removes the original texture's own colour
    // Transfer:       0 .. 1   applies the secondary input's colour
    // Smoothing:      0 .. 32  filters (blurs) the secondary colour field
    // ChromaRestore:  0 .. 2   restores / boosts colour in coloured regions
    // =====================================================================

    private bool _isColorOverrideEnabled = false;
    private float _amount = 1f;
    private float _decolorize = 1f;
    private float _transfer = 1f;
    private int _smoothing = 0;
    private float _chromaRestore = 1f;

    public bool IsColorOverrideEnabled
    {
        get => _isColorOverrideEnabled;
        set => SetPropertyTriggerRecalculation(ref _isColorOverrideEnabled, value);
    }

    public float Amount
    {
        get => _amount;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (SetCallerPropertyReturn(ref _amount, clamped))
            {
                OnPropertyChanged(nameof(AmountPercent));
                _ = TriggerRecalculation();
            }
        }
    }

    public float AmountPercent
    {
        get => _amount * 100f;
        set => Amount = Math.Clamp(value, 0f, 100f) / 100f;
    }

    public float Decolorize
    {
        get => _decolorize;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (SetCallerPropertyReturn(ref _decolorize, clamped))
            {
                OnPropertyChanged(nameof(DecolorizePercent));
                _ = TriggerRecalculation();
            }
        }
    }

    public float DecolorizePercent
    {
        get => _decolorize * 100f;
        set => Decolorize = Math.Clamp(value, 0f, 100f) / 100f;
    }

    public float Transfer
    {
        get => _transfer;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (SetCallerPropertyReturn(ref _transfer, clamped))
            {
                OnPropertyChanged(nameof(TransferPercent));
                _ = TriggerRecalculation();
            }
        }
    }

    public float TransferPercent
    {
        get => _transfer * 100f;
        set => Transfer = Math.Clamp(value, 0f, 100f) / 100f;
    }

    public int Smoothing
    {
        get => _smoothing;
        set => SetPropertyTriggerRecalculation(ref _smoothing, Math.Clamp(value, 0, 32));
    }

    public float ChromaRestore
    {
        get => _chromaRestore;
        set
        {
            float clamped = Math.Clamp(value, 0f, 2f);
            if (SetCallerPropertyReturn(ref _chromaRestore, clamped))
            {
                OnPropertyChanged(nameof(ChromaRestorePercent));
                _ = TriggerRecalculation();
            }
        }
    }

    public float ChromaRestorePercent
    {
        get => _chromaRestore * 100f;
        set => ChromaRestore = Math.Clamp(value, 0f, 200f) / 100f;
    }

    // =====================================================================
    // Secondary image loading
    // =====================================================================

    public void LoadSecondaryImage(IWriteableBitmap bitmap)
    {
        SecondaryImage = PrepareAndResizeImage(bitmap);

        _modificationHelper.PixelsSecondary = ExtractPixels(SecondaryImage);
        _modificationHelper.SecondaryWidth = SecondaryImage.PixelWidth;
        _modificationHelper.SecondaryHeight = SecondaryImage.PixelHeight;

        SecondaryInitTextVisible = false;

        // Loading a secondary implies the user wants to use the effect.
        if (!_isColorOverrideEnabled)
            IsColorOverrideEnabled = true;
        else
            _ = TriggerRecalculation();
    }

    private IWriteableBitmap PrepareAndResizeImage(IWriteableBitmap bitmap)
    {
        var image = bitmap.HasAlpha
            ? _mediaFactory.CloneBitmap(bitmap)
            : _bitmapOperations.ConvertRGB24ToBGRA32(bitmap);

        return Resize(image);
    }

    private IWriteableBitmap Resize(IWriteableBitmap image)
    {
        int width = Math.Clamp(image.PixelWidth, MIN_SIZE, MAX_SIZE);
        int height = Math.Clamp(image.PixelHeight, MIN_SIZE, MAX_SIZE);

        if (width == image.PixelWidth && height == image.PixelHeight)
            return image;

        return _bitmapOperations.ResizeScaled(image, width, height);
    }

    private static byte[] ExtractPixels(IWriteableBitmap image)
    {
        var pixels = new byte[image.PixelWidth * image.PixelHeight * BPP];
        image.CopyPixels(pixels, image.PixelWidth * BPP, 0);
        return pixels;
    }

    public void ResetState()
    {
        _secondaryImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        OnPropertyChanged(nameof(SecondaryImage));

        SecondaryInitTextVisible = true;

        _isColorOverrideEnabled = false;
        _amount = 1f;
        _decolorize = 1f;
        _transfer = 1f;
        _smoothing = 0;
        _chromaRestore = 1f;

        OnPropertyChanged(nameof(IsColorOverrideEnabled));
        OnPropertyChanged(nameof(AmountPercent));
        OnPropertyChanged(nameof(DecolorizePercent));
        OnPropertyChanged(nameof(TransferPercent));
        OnPropertyChanged(nameof(Smoothing));
        OnPropertyChanged(nameof(ChromaRestorePercent));
    }

    private void ConfigureHelper()
    {
        _modificationHelper.ColorOverrideEnabled = _isColorOverrideEnabled;
        _modificationHelper.ColorOverrideAmount = _amount;
        _modificationHelper.ColorOverrideDecolorize = _decolorize;
        _modificationHelper.ColorOverrideTransfer = _transfer;
        _modificationHelper.ColorOverrideSmoothing = _smoothing;
        _modificationHelper.ColorOverrideChromaRestore = _chromaRestore;
    }

    protected override async Task TriggerRecalculation()
    {
        await _modificationHelper.QueueRecalc(ConfigureHelper);
    }
}
