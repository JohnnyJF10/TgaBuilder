using TgaBuilderLib.Transitions;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel;

public class EdgeViewModel : ThrottledViewModelBase
{
    public EdgeViewModel(
        ITransitionHelper transitionHelper,
        TransitionInViewModel transitionInVM)
    {
        _transitionHelper = transitionHelper;
        TransitionInVM = transitionInVM;
    }

    public TransitionInViewModel TransitionInVM { get; }

    private ITransitionHelper _transitionHelper;

    private EdgeBlendMode _blendMode = EdgeBlendMode.Multiply;
    private int _edgeWidth = 1;

    public EdgeBlendMode BlendMode
    {
        get => _blendMode;
        set => SetPropertyTriggerRecalculation(ref _blendMode, value);
    }

    public int EdgeWidth
    {
        get => _edgeWidth;
        set => SetPropertyTriggerRecalculation(ref _edgeWidth, value);
    }

    public Array EdgeBlendModes => Enum.GetValues(typeof(EdgeBlendMode));

    private void ConfigureTransitionHelper()
    {
        _transitionHelper.CurrentBricksPipelineRequirements
        = BricksPipelineRequirements.RequiresDrawing;

        _transitionHelper.BlendMode = BlendMode;
        _transitionHelper.EdgeWidth = EdgeWidth;
    }

    protected override async Task TriggerRecalculation()
    {
        await _transitionHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}

