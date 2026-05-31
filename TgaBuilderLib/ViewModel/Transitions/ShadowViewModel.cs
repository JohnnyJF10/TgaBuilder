using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;
using TgaBuilderLib.ViewModel.Transitions;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick transition shadow section.
/// Controls shadow color, size, hardness, and eyedropper mode.
/// </summary>
public class TransitionShadowViewModel : ThrottledViewModelBase
{
    public TransitionShadowViewModel(
        ITransitionHelper transitionHelper,
        TransitionPresentersViewModel presenters)
    {
        _transitionHelper = transitionHelper;
        Presenters = presenters;
    }

    private readonly ITransitionHelper _transitionHelper;
    public TransitionPresentersViewModel Presenters { get; }

    // =====================================================================
    // Fields
    // =====================================================================

    private Color _shadowColor = new Color(42, 42, 42, 42);
    private int _shadowSize = 3;
    private int _shadowHardness = 50;
    private bool _isShadowEyedropperMode;

    // =====================================================================
    // Properties
    // =====================================================================

    public Color ShadowColor
    {
        get => _shadowColor;
        set => SetPropertyTriggerRecalculation(ref _shadowColor, value);
    }

    public int ShadowSize
    {
        get => _shadowSize;
        set => SetPropertyTriggerRecalculation(ref _shadowSize, value);
    }

    public int ShadowHardness
    {
        get => _shadowHardness;
        set => SetPropertyTriggerRecalculation(ref _shadowHardness, value);
    }

    public bool IsShadowEyedropperMode
    {
        get => _isShadowEyedropperMode;
        set => SetCallerProperty(ref _isShadowEyedropperMode, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresDrawing;
        _transitionHelper.ShadowColor = _shadowColor;
        _transitionHelper.ShadowSize = _shadowSize;
        _transitionHelper.ShadowHardness = _shadowHardness;
        return true;
    }

    protected override void Recalculate()
    {
        // Recalculation is driven by the parent TransitionViewModel
    }
}
