using TgaBuilderLib.Enums;
using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel;

// =========================================================================
// RetrofierViewModel
//
// Drives the "Texture Retrofier" expander: an optional utility that recreates
// the limited colour space of TR1 / TR2 (Sega Saturn) textures through
// per-channel quantization, an optional reduced palette and ordered
// (checkerboard / Bayer) dithering. Runs at the very end of the pipeline.
// =========================================================================

public class RetrofierViewModel : ThrottledViewModelBase
{
    public RetrofierViewModel(
        IModificationsHelper modificationHelper,
        ModificationInViewModel modificationInVM)
    {
        _modificationHelper = modificationHelper;
        ModificationInVM = modificationInVM;
    }

    public ModificationInViewModel ModificationInVM { get; }

    private readonly IModificationsHelper _modificationHelper;

    // =====================================================================
    // Quantization
    // =====================================================================

    private RetroQuantizationLevel _quantization = RetroQuantizationLevel.None;

    public int SelectedQuantizationIndex
    {
        get => (int)_quantization;
        set
        {
            var level = (RetroQuantizationLevel)value;
            if (_quantization != level)
            {
                _quantization = level;
                OnPropertyChanged(nameof(SelectedQuantizationIndex));
                _ = TriggerRecalculation();
            }
        }
    }

    // =====================================================================
    // Palette limitation
    // =====================================================================

    private bool _isPaletteLimitEnabled = false;
    private int _maxColors = 16;

    public bool IsPaletteLimitEnabled
    {
        get => _isPaletteLimitEnabled;
        set => SetPropertyTriggerRecalculation(ref _isPaletteLimitEnabled, value);
    }

    public int MaxColors
    {
        get => _maxColors;
        set => SetPropertyTriggerRecalculation(ref _maxColors, Math.Clamp(value, 2, 256));
    }

    // =====================================================================
    // Dithering
    // =====================================================================

    private RetroDitherMode _ditherMode = RetroDitherMode.None;
    private float _ditherStrength = 1f;
    private int _ditherCellSize = 1;

    public int SelectedDitherModeIndex
    {
        get => (int)_ditherMode;
        set
        {
            var mode = (RetroDitherMode)value;
            if (_ditherMode != mode)
            {
                _ditherMode = mode;
                OnPropertyChanged(nameof(SelectedDitherModeIndex));
                OnPropertyChanged(nameof(IsDitherActive));
                _ = TriggerRecalculation();
            }
        }
    }

    public bool IsDitherActive => _ditherMode != RetroDitherMode.None;

    public float DitherStrength
    {
        get => _ditherStrength;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (SetCallerPropertyReturn(ref _ditherStrength, clamped))
            {
                OnPropertyChanged(nameof(DitherStrengthPercent));
                _ = TriggerRecalculation();
            }
        }
    }

    public float DitherStrengthPercent
    {
        get => _ditherStrength * 100f;
        set => DitherStrength = Math.Clamp(value, 0f, 100f) / 100f;
    }

    public int DitherCellSize
    {
        get => _ditherCellSize;
        set => SetPropertyTriggerRecalculation(ref _ditherCellSize, Math.Clamp(value, 1, 8));
    }

    // =====================================================================
    // Recalculation
    // =====================================================================

    private void ConfigureHelper()
    {
        _modificationHelper.RetroQuantization = _quantization;
        _modificationHelper.RetroPaletteLimitEnabled = _isPaletteLimitEnabled;
        _modificationHelper.RetroMaxColors = _maxColors;
        _modificationHelper.RetroDitherMode = _ditherMode;
        _modificationHelper.RetroDitherStrength = _ditherStrength;
        _modificationHelper.RetroDitherCellSize = _ditherCellSize;
    }

    protected override async Task TriggerRecalculation()
    {
        await _modificationHelper.QueueRecalc(ConfigureHelper);
    }
}
