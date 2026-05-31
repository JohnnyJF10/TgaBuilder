using System;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;
using TgaBuilderLib.Modifications;
using TgaBuilderLib.ViewModel.Modifications;

namespace TgaBuilderLib.ViewModel.Modifications;

/// <summary>
/// Child VM for the Modifications color overlay section.
/// Controls overlay color, amount, mix mode, and mode-specific parameters.
/// </summary>
public class ModificationsColorOverlayViewModel : ThrottledViewModelBase
{
    public ModificationsColorOverlayViewModel(
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

    private Color _colorOverlay = new(0, 0, 0, 0);
    private float _colorOverlayAmount;
    private int _selectedColorOverlayMixModeIndex = (int)ColorOverlayMixMode.OklabChroma;
    private float _colorOverlaySoftLightStrength = 1f;
    private float _colorOverlayLumaPreservation = 1f;
    private float _colorOverlayChromaBoost = 1f;
    private bool _isColorOverlayEyedropperMode;

    // =====================================================================
    // Properties
    // =====================================================================

    public Color ColorOverlay
    {
        get => _colorOverlay;
        set => SetPropertyTriggerRecalculation(ref _colorOverlay, value);
    }

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

    public bool IsColorOverlayEyedropperMode
    {
        get => _isColorOverlayEyedropperMode;
        set => SetCallerProperty(ref _isColorOverlayEyedropperMode, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _modificationsHelper.ColorOverlay = _colorOverlay;
        _modificationsHelper.ColorOverlayAmount = _colorOverlayAmount;
        _modificationsHelper.ColorOverlayMixMode = (ColorOverlayMixMode)Math.Clamp(
            _selectedColorOverlayMixModeIndex,
            (int)ColorOverlayMixMode.Linear,
            (int)ColorOverlayMixMode.OklabChroma);
        _modificationsHelper.ColorOverlaySoftLightStrength = _colorOverlaySoftLightStrength;
        _modificationsHelper.ColorOverlayLumaPreservation = _colorOverlayLumaPreservation;
        _modificationsHelper.ColorOverlayChromaBoost = _colorOverlayChromaBoost;
        return true;
    }

    protected override void Recalculate()
    {
        // Recalculation is driven by the parent ModificationsViewModel
    }
}
