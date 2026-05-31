using TgaBuilderLib.Modifications;
using TgaBuilderLib.ViewModel.Modifications;

namespace TgaBuilderLib.ViewModel.Modifications;

/// <summary>
/// Child VM for the Modifications basic adjustments section.
/// Controls exposure, brightness, contrast, highlights, shadows, whites, blacks.
/// </summary>
public class ModificationsBasicViewModel : ThrottledViewModelBase
{
    public ModificationsBasicViewModel(
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

    private float _exposure;
    private float _brightness;
    private float _contrast;
    private float _highlights;
    private float _shadows;
    private float _whites;
    private float _blacks;

    // =====================================================================
    // Properties
    // =====================================================================

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

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

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
        // Recalculation is driven by the parent ModificationsViewModel
    }
}
