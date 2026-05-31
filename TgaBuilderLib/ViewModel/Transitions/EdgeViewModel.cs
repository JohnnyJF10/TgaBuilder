using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;
using TgaBuilderLib.ViewModel.Transitions;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick transition edge coloring section.
/// Controls edge color, blend mode, edge width, and eyedropper mode.
/// </summary>
public class TransitionEdgeViewModel : ThrottledViewModelBase
{
    public TransitionEdgeViewModel(
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

    private Color _edgeColor = new Color(255, 255, 255, 128);
    private EdgeBlendMode _blendMode = EdgeBlendMode.Multiply;
    private int _edgeWidth = 1;
    private bool _isEyedropperMode;

    // =====================================================================
    // Properties
    // =====================================================================

    public Color EdgeColor
    {
        get => _edgeColor;
        set => SetPropertyTriggerRecalculation(ref _edgeColor, value);
    }

    public EdgeBlendMode BlendMode
    {
        get => _blendMode;
        set => SetPropertyTriggerRecalculation(ref _blendMode, value);
    }

    public System.Array EdgeBlendModes => System.Enum.GetValues(typeof(EdgeBlendMode));

    public int EdgeWidth
    {
        get => _edgeWidth;
        set => SetPropertyTriggerRecalculation(ref _edgeWidth, value);
    }

    public bool IsEyedropperMode
    {
        get => _isEyedropperMode;
        set => SetCallerProperty(ref _isEyedropperMode, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresDrawing;
        _transitionHelper.EdgeColor = _edgeColor;
        _transitionHelper.BlendMode = _blendMode;
        _transitionHelper.EdgeWidth = _edgeWidth;
        return true;
    }

    protected override void Recalculate()
    {
        // Recalculation is driven by the parent TransitionViewModel
    }
}
