using TgaBuilderLib.Transitions;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel;

public class UnderfillingViewModel : ThrottledViewModelBase
{
    public UnderfillingViewModel(
        ITransitionHelper transitionHelper,
        TransitionsPresentersViewModel transitionsPresentersVM)
    {
        _transitionHelper = transitionHelper;
        TransitionsPresentersVM = transitionsPresentersVM;
    }

    public TransitionsPresentersViewModel TransitionsPresentersVM { get; }

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
        = BricksPipelineRequirements.RequiresDrawing;

        _transitionHelper.UnderfillingPivot = UnderfillingPivot;
        _transitionHelper.ReverseUnderfilling = ReverseUnderfilling;
        _transitionHelper.UnderfillingThreshold = UnderfillingThreshold;
    }

    protected override bool PreProcess()
    {
        return TransitionsPresentersVM.DoPreProcessing();
    }

    protected override async Task Recalculate()
    {
        ConfigureTransitionHelper();
 
        await TransitionsPresentersVM.DoRecalculation();
    }
}

