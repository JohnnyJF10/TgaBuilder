using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel.Modifications;

/// <summary>
/// Child VM for the Modifications basic adjustments section.
/// Controls exposure, brightness, contrast, highlights, shadows, whites, blacks.
/// </summary>
public class ModificationsBasicViewModel : ThrottledViewModelBase
{
    public ModificationsBasicViewModel(
        IMediaFactory mediaFactory,
        IModificationsHelper modificationsHelper,
        ModificationsPresentersViewModel presenters)
    {
        _mediaFactory = mediaFactory;
        _modificationsHelper = modificationsHelper;
        Presenters = presenters;
    }

    private readonly IMediaFactory _mediaFactory;
    private readonly IModificationsHelper _modificationsHelper;
    public ModificationsPresentersViewModel Presenters { get; }

    private float _exposure;
    private float _brightness;
    private float _contrast;
    private float _highlights;
    private float _shadows;
    private float _whites;
    private float _blacks;

    public float Exposure
    {
        get => _exposure;
        set => SetPropertyTriggerRecalculation(ref _exposure, value);
    }

    public float Brightness
    {
        get => _brightness;
        set => SetPropertyTriggerRecalculation(ref _brightness, value);
    }

    public float Contrast
    {
        get => _contrast;
        set => SetPropertyTriggerRecalculation(ref _contrast, value);
    }

    public float Highlights
    {
        get => _highlights;
        set => SetPropertyTriggerRecalculation(ref _highlights, value);
    }

    public float Shadows
    {
        get => _shadows;
        set => SetPropertyTriggerRecalculation(ref _shadows, value);
    }

    public float Whites
    {
        get => _whites;
        set => SetPropertyTriggerRecalculation(ref _whites, value);
    }

    public float Blacks
    {
        get => _blacks;
        set => SetPropertyTriggerRecalculation(ref _blacks, value);
    }

    protected override bool DoPreProcessing()
    {
        _modificationsHelper.Exposure = _exposure;
        _modificationsHelper.Brightness = _brightness;
        _modificationsHelper.Contrast = _contrast;
        _modificationsHelper.Highlights = _highlights;
        _modificationsHelper.Shadows = _shadows;
        _modificationsHelper.Whites = _whites;
        _modificationsHelper.Blacks = _blacks;
        return true;
    }

    protected override void Recalculate()
    {
        var pixels = Presenters.InputPixels;
        if (pixels == null || pixels.Length == 0) return;

        int w = Presenters.InputImage.PixelWidth;
        int h = Presenters.InputImage.PixelHeight;
        if (w == 0 || h == 0) return;

        _modificationsHelper.Width = w;
        _modificationsHelper.Height = h;

        byte[] inputCopy = new byte[pixels.Length];
        System.Array.Copy(pixels, inputCopy, pixels.Length);

        byte[] result = _modificationsHelper.Apply(inputCopy);

        var resultBmp = _mediaFactory.CreateBitmapFromRaw(w, h, true, result, w * 4);
        Presenters.ResultImage = _mediaFactory.CloneBitmap(resultBmp);
    }
}
