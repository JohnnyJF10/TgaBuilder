using TrLynxLib.Enums;
using TrLynxLib.Modifications;

namespace TrLynxLib.ViewModel;

public class ColorOverlayViewModel : ThrottledViewModelBase
{
    public ColorOverlayViewModel(
        IModificationsHelper modificationHelper,
        ModificationsInViewModel modificationInVM)
    {
        _modificationHelper = modificationHelper;
        ModificationInVM = modificationInVM;
    }

    public ModificationsInViewModel ModificationInVM { get; }

    private IModificationsHelper _modificationHelper;

    // =====================================================================
    // Color overlay adjustments
    // [Color:        RGBA(0..255)      In the ModificationInVm, needs to be in the same class as the bitmap]
    // Amount:       0 .. 1
    // Mix Mode:     Linear, Soft Light, Oklab Chroma
    // Soft Light Strength: 0 .. 2 (only for Soft Light mode)
    // Luma Preservation: 0 .. 2 (only for Oklab Chroma mode)
    // Chroma Boost: 0 .. 2 (only for Oklab Chroma mode)
    // =====================================================================

    private float _colorOverlayAmount = 0f;
    private int _selectedColorOverlayMixModeIndex = (int)ColorOverlayMixMode.OklabChroma;
    private float _colorOverlaySoftLightStrength = 1f;
    private float _colorOverlayLumaPreservation = 1f;
    private float _colorOverlayChromaBoost = 1f;


    public float ColorOverlayAmount
    {
        get => _colorOverlayAmount;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_colorOverlayAmount - clamped) > float.Epsilon)
            {
                _colorOverlayAmount = clamped;
                OnPropertyChanged(nameof(ColorOverlayAmount));
                OnPropertyChanged(nameof(ColorOverlayBlendPercent));
                _ = TriggerRecalculation();
            }
        }
    }

    public float ColorOverlayBlendPercent
    {
        get => _colorOverlayAmount * 100f;
        set => ColorOverlayAmount = Math.Clamp(value, 0f, 100f) / 100f;
    }

    public int SelectedColorOverlayMixModeIndex
    {
        get => _selectedColorOverlayMixModeIndex;
        set
        {
            if (_selectedColorOverlayMixModeIndex != value)
            {
                _selectedColorOverlayMixModeIndex = value;
                OnPropertyChanged(nameof(SelectedColorOverlayMixModeIndex));
                OnPropertyChanged(nameof(IsColorOverlaySoftLightMode));
                OnPropertyChanged(nameof(IsColorOverlayOklabMode));
                _ = TriggerRecalculation();
            }
        }
    }

    public float ColorOverlaySoftLightStrength
    {
        get => _colorOverlaySoftLightStrength;
        set => SetPropertyTriggerRecalculation(ref _colorOverlaySoftLightStrength, value);
    }

    public float ColorOverlayLumaPreservation
    {
        get => _colorOverlayLumaPreservation;
        set => SetPropertyTriggerRecalculation(ref _colorOverlayLumaPreservation, value);
    }

    public float ColorOverlayChromaBoost
    {
        get => _colorOverlayChromaBoost;
        set => SetPropertyTriggerRecalculation(ref _colorOverlayChromaBoost, value);
    }

    public bool IsColorOverlaySoftLightMode
        => SelectedColorOverlayMixModeIndex == (int)ColorOverlayMixMode.SoftLight;

    public bool IsColorOverlayOklabMode
        => SelectedColorOverlayMixModeIndex == (int)ColorOverlayMixMode.OklabChroma;



    private void ConfigureTransitionHelper()
    {
        _modificationHelper.ColorOverlayAmount = _colorOverlayAmount;
        _modificationHelper.ColorOverlayMixMode = (ColorOverlayMixMode)Math.Clamp(
            _selectedColorOverlayMixModeIndex,
            (int)ColorOverlayMixMode.Linear,
            (int)ColorOverlayMixMode.OklabChroma);
        _modificationHelper.ColorOverlaySoftLightStrength = _colorOverlaySoftLightStrength;
        _modificationHelper.ColorOverlayLumaPreservation = _colorOverlayLumaPreservation;
        _modificationHelper.ColorOverlayChromaBoost = _colorOverlayChromaBoost;
    }

    protected override async Task TriggerRecalculation()
    {
        await _modificationHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}