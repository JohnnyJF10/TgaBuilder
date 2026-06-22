using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class UnderfillingViewModel : ThrottledViewModelBase
{
    public UnderfillingViewModel(
        ITransitionHelper transitionHelper,
        TransitionInViewModel transitionInVM)
    {
        _transitionHelper = transitionHelper;
        TransitionInVM = transitionInVM;
    }

    public TransitionInViewModel TransitionInVM { get; }

    private ITransitionHelper _transitionHelper;



    private float _underfillingPivot = 0.5f;
    private bool _reverseUnderfilling = false;
    private int _underFillingThreshold = 0;



    public float UnderfillingPivot
    {
        get => _underfillingPivot;
        set => SetPropertyTriggerRecalculation(ref _underfillingPivot, value);
    }

    public bool ReverseUnderfilling
    {
        get => _reverseUnderfilling;
        set => SetPropertyTriggerRecalculation(ref _reverseUnderfilling, value);
    }

    public int UnderfillingThreshold
    {
        get => _underFillingThreshold;
        set => SetPropertyTriggerRecalculation(ref _underFillingThreshold, value);
    }

    private void ConfigureTransitionHelper()
    {
        _transitionHelper.CurrentBricksPipelineRequirements
        = BricksPipelineRequirements.RequiresSelectionBuilding;

        _transitionHelper.UnderfillingPivot = UnderfillingPivot;
        _transitionHelper.ReverseUnderfilling = ReverseUnderfilling;
        _transitionHelper.UnderfillingThreshold = UnderfillingThreshold;
    }

    protected override async Task TriggerRecalculation()
    {
        await _transitionHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}

