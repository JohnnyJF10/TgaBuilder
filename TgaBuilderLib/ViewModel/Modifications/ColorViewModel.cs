using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel.Modifications;

/// <summary>
/// Child VM for the Modifications color adjustments section.
/// Controls saturation, vibrance, hue, temperature, tint.
/// </summary>
public class ModificationsColorViewModel : ThrottledViewModelBase
{
    public ModificationsColorViewModel(
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

    private float _saturation;
    private float _vibrance;
    private float _hue;
    private float _temperature;
    private float _tint;

    public float Saturation
    {
        get => _saturation;
        set => SetPropertyTriggerRecalculation(ref _saturation, value);
    }

    public float Vibrance
    {
        get => _vibrance;
        set => SetPropertyTriggerRecalculation(ref _vibrance, value);
    }

    public float Hue
    {
        get => _hue;
        set => SetPropertyTriggerRecalculation(ref _hue, value);
    }

    public float Temperature
    {
        get => _temperature;
        set => SetPropertyTriggerRecalculation(ref _temperature, value);
    }

    public float Tint
    {
        get => _tint;
        set => SetPropertyTriggerRecalculation(ref _tint, value);
    }

    protected override bool DoPreProcessing()
    {
        _modificationsHelper.Saturation = _saturation;
        _modificationsHelper.Vibrance = _vibrance;
        _modificationsHelper.Hue = _hue;
        _modificationsHelper.Temperature = _temperature;
        _modificationsHelper.Tint = _tint;
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
