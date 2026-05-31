using TgaBuilderLib.Modifications;
using TgaBuilderLib.ViewModel.Modifications;

namespace TgaBuilderLib.ViewModel.Modifications;

/// <summary>
/// Child VM for the Modifications color adjustments section.
/// Controls saturation, vibrance, hue, temperature, tint.
/// </summary>
public class ModificationsColorViewModel : ThrottledViewModelBase
{
    public ModificationsColorViewModel(
        IModificationsHelper modificationsHelper,
        ModificationsPresentersViewModel presenters)
    {
        _modificationsHelper = modificationsHelper;
        Presenters = presenters;
    }

    private readonly IModificationsHelper _modificationsHelper;
    public ModificationsPresentersViewModel Presenters { get; }

    // =====================================================================
    // Fields
    // =====================================================================

    private float _saturation;
    private float _vibrance;
    private float _hue;
    private float _temperature;
    private float _tint;

    // =====================================================================
    // Properties
    // =====================================================================

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

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

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
        // Recalculation is driven by the parent ModificationsViewModel
    }
}
