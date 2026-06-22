using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel;

public class ColorViewModel : ThrottledViewModelBase
{
    public ColorViewModel(
        IModificationsHelper modificationHelper,
        ModificationsInViewModel modificationInVM)
    {
        _modificationHelper = modificationHelper;
        ModificationInVM = modificationInVM;
    }

    public ModificationsInViewModel ModificationInVM { get; }

    private IModificationsHelper _modificationHelper;

    // =====================================================================
    // Color adjustments
    // Saturation:   -1 .. +1
    // Vibrance:     -1 .. +1
    // Hue:          -180 .. +180 (degrees)
    // Temperature:  -1 .. +1
    // Tint:         -1 .. +1
    // =====================================================================

    private float _saturation = 0f;
    private float _vibrance = 0f;
    private float _hue = 0f;
    private float _temperature = 0f;
    private float _tint = 0f;

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

    private void ConfigureTransitionHelper()
    {
        _modificationHelper.Saturation = _saturation;
        _modificationHelper.Vibrance = _vibrance;
        _modificationHelper.Hue = _hue;
        _modificationHelper.Temperature = _temperature;
        _modificationHelper.Tint = _tint;
    }

    protected override async Task TriggerRecalculation()
    {
        await _modificationHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}