using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class PivotViewModel : ThrottledViewModelBase
{
    public PivotViewModel(
        ITransitionHelper transitionHelper,
        TransitionInViewModel transitionInVM)
    {
        _transitionHelper = transitionHelper;
        TransitionInVM = transitionInVM;
    }

    public TransitionInViewModel TransitionInVM { get; }

    private ITransitionHelper _transitionHelper;


    private TransitionDirection _selectedTransitionMode = TransitionDirection.Top;
    private float _pivotValue = 0.5f;

    private float _blendHardnessValue = 0.5f;

    private float _wideningValue = 0f;
    private float _shiftValue = 0f;

    private bool _reversePivot;
    private bool _sliceCornerTiles;
    private bool _protectEdges = true;




    public TransitionDirection SelectedTransitionMode
    {
        get => _selectedTransitionMode;
        set => SetPropertyTriggerRecalculation(ref _selectedTransitionMode, value);
    }

    public float PivotValue
    {
        get => _pivotValue;
        set => SetPropertyTriggerRecalculation(ref _pivotValue, value);
    }


    public float BlendHardnessValue
    {
        get => _blendHardnessValue;
        set => SetPropertyTriggerRecalculation(ref _blendHardnessValue, value);
    }

    public float WideningValue
    {
        get => _wideningValue;
        set => SetPropertyTriggerRecalculation(ref _wideningValue, value);
    }

    public float ShiftValue
    {
        get => _shiftValue;
        set => SetPropertyTriggerRecalculation(ref _shiftValue, value);
    }

    public bool ReversePivot
    {
        get => _reversePivot;
        set => SetPropertyTriggerRecalculation(ref _reversePivot, value);
    }

    public bool SliceCornerTiles
    {
        get => _sliceCornerTiles;
        set => SetPropertyTriggerRecalculation(ref _sliceCornerTiles, value);
    }

    public bool ProtectEdges
    {
        get => _protectEdges;
        set => SetPropertyTriggerRecalculation(ref _protectEdges, value);
    }

    private void ConfigureTransitionHelper()
    {
        _transitionHelper.CurrentBricksPipelineRequirements
        = BricksPipelineRequirements.RequiresSelectionBuilding;

        _transitionHelper.Direction = SelectedTransitionMode;
        _transitionHelper.Pivot = PivotValue;
        _transitionHelper.Hardness = BlendHardnessValue;
        _transitionHelper.Widening = WideningValue;
        _transitionHelper.Shift = ShiftValue;
        _transitionHelper.ReversePivot = ReversePivot;
        _transitionHelper.SliceCornerTiles = SliceCornerTiles;
        _transitionHelper.ProtectEdges = ProtectEdges;
    }

    protected override async Task TriggerRecalculation()
    {
        await _transitionHelper.QueueRecalc(ConfigureTransitionHelper);
    }
}