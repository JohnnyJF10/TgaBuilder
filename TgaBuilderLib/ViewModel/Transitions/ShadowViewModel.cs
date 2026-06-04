using TgaBuilderLib.Transitions;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel;

public class ShadowViewModel : ThrottledViewModelBase
{
    public ShadowViewModel(
        ITransitionHelper transitionHelper,
        TransitionInViewModel transitionInVM)
    {
        _transitionHelper = transitionHelper;
        TransitionInVM = transitionInVM;
    }

    public TransitionInViewModel TransitionInVM { get; }

    private ITransitionHelper _transitionHelper;



    private int _shadowSize = 3;
    private int _shadowHardness = 50;

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

    private void ConfigureTransitionHelper()
    {
        _transitionHelper.CurrentBricksPipelineRequirements 
        = BricksPipelineRequirements.RequiresDrawing;

        _transitionHelper.ShadowSize = ShadowSize;
        _transitionHelper.ShadowHardness = ShadowHardness;
    }

    protected override async Task TriggerRecalculation()
    {
        await _transitionHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}

