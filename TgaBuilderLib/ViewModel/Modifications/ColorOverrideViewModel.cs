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
        ModificationsInViewModel modificationInVM)
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

    public ModificationsInViewModel ModificationInVM { get; }

    // =====================================================================
    // Secondary input image
    // =====================================================================

    private IWriteableBitmap _secondaryImage;
    private bool _secondaryInitTextVisible = true;

    // The loaded secondary texture at its own (clamped) resolution. Kept so it can
    // be re-fitted to the input size whenever the input picture changes size.
    private IWriteableBitmap? _secondarySource;

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
        _secondarySource = PrepareSecondarySource(bitmap);
        SecondaryImage = _secondarySource;
        SecondaryInitTextVisible = false;

        SyncSecondaryToBuffer();

        // Loading a secondary implies the user wants to use the effect.
        if (!_isColorOverrideEnabled)
            IsColorOverrideEnabled = true;
        else
            _ = TriggerRecalculation();
    }

    // Re-fits the loaded secondary texture to the (possibly new) input size and
    // copies it into the helper buffer. Called by the owning view model after the
    // input picture — and therefore the buffer set — has changed size.
    public void OnInputResized()
    {
        if (_secondarySource is null)
            return;

        SyncSecondaryToBuffer();
        _ = TriggerRecalculation();
    }

    private void SyncSecondaryToBuffer()
    {
        if (_secondarySource is null || !_modificationHelper.IsActive)
            return;

        int width = _modificationHelper.Width;
        int height = _modificationHelper.Height;

        var fitted = (_secondarySource.PixelWidth == width && _secondarySource.PixelHeight == height)
            ? _secondarySource
            : _bitmapOperations.ResizeScaled(_secondarySource, width, height);

        _modificationHelper.PixelsSecondary = CopyPixelsInto(fitted, _modificationHelper.PixelsSecondary);
        _modificationHelper.ColorOverrideHasSecondary = true;
    }

    private IWriteableBitmap PrepareSecondarySource(IWriteableBitmap bitmap)
    {
        var image = bitmap.HasAlpha
            ? _mediaFactory.CloneBitmap(bitmap)
            : _bitmapOperations.ConvertRGB24ToBGRA32(bitmap);

        return ClampSize(image);
    }

    private IWriteableBitmap ClampSize(IWriteableBitmap image)
    {
        int width = Math.Clamp(image.PixelWidth, MIN_SIZE, MAX_SIZE);
        int height = Math.Clamp(image.PixelHeight, MIN_SIZE, MAX_SIZE);

        if (width == image.PixelWidth && height == image.PixelHeight)
            return image;

        return _bitmapOperations.ResizeScaled(image, width, height);
    }

    // Copies the image pixels into the helper's reusable secondary buffer, reusing
    // it in place when the size already matches.
    private static byte[] CopyPixelsInto(IWriteableBitmap image, byte[] target)
    {
        int stride = image.PixelWidth * BPP;
        int needed = stride * image.PixelHeight;

        if (target.Length != needed)
            target = new byte[needed];

        image.CopyPixels(target, stride, 0);
        return target;
    }

    public void ResetState()
    {
        _secondarySource = null;
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
