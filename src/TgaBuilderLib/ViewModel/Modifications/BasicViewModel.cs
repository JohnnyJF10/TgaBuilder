using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel;

public class BasicViewModel : ThrottledViewModelBase
{
    public BasicViewModel(
        IModificationsHelper modificationHelper,
        ModificationsInViewModel modificationInVM)
    {
        _modificationHelper = modificationHelper;
        ModificationInVM = modificationInVM;
    }

    public ModificationsInViewModel ModificationInVM { get; }

    private IModificationsHelper _modificationHelper;


    // =====================================================================
    // Basic adjustments
    // Exposure:    -5 .. +5 (stops)
    // Brightness:  -1 .. +1
    // Contrast:    -1 .. +1
    // Highlights:  -1 .. +1
    // Shadows:     -1 .. +1
    // Whites:      -1 .. +1
    // Blacks:      -1 .. +1
    // =====================================================================

    private float _exposure = 0f;
    private float _brightness = 0f;
    private float _contrast = 0f;
    private float _highlights = 0f;
    private float _shadows = 0f;
    private float _whites = 0f;
    private float _blacks = 0f;

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
    

    private void ConfigureTransitionHelper()
    {
        _modificationHelper.Exposure = _exposure;
        _modificationHelper.Brightness = _brightness;
        _modificationHelper.Contrast = _contrast;
        _modificationHelper.Highlights = _highlights;
        _modificationHelper.Shadows = _shadows;
        _modificationHelper.Whites = _whites;
        _modificationHelper.Blacks = _blacks;
    }

    protected override async Task TriggerRecalculation()
    {
        await _modificationHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}