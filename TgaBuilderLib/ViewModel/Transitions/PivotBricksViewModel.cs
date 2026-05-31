using TgaBuilderLib.Transitions;
using TgaBuilderLib.ViewModel.Transitions;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick transition pivot/selection section.
/// Controls pivot, widening, shift, reverse, corner slicing, edge protection.
/// </summary>
public class TransitionPivotBricksViewModel : ThrottledViewModelBase
{
    public TransitionPivotBricksViewModel(
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

    private TransitionMode _selectedTransitionMode = TransitionMode.Top;
    private float _pivotValue = 0.5f;
    private float _wideningValue = 0f;
    private float _shiftValue = 0f;
    private bool _reversePivot;
    private bool _sliceCornerTiles;
    private bool _protectEdges = true;

    // =====================================================================
    // Properties
    // =====================================================================

    public TransitionMode SelectedTransitionMode
    {
        get => _selectedTransitionMode;
        set => SetPropertyTriggerRecalculation(ref _selectedTransitionMode, value);
    }

    public float PivotValue
    {
        get => _pivotValue;
        set => SetPropertyTriggerRecalculation(ref _pivotValue, value);
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

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;
        _transitionHelper.Mode = _selectedTransitionMode;
        _transitionHelper.Pivot = _pivotValue;
        _transitionHelper.Widening = _wideningValue;
        _transitionHelper.Shift = _shiftValue;
        _transitionHelper.ReversePivot = _reversePivot;
        _transitionHelper.SliceCornerTiles = _sliceCornerTiles;
        _transitionHelper.ProtectEdges = _protectEdges;
        return true;
    }

    protected override void Recalculate()
    {
        // Recalculation is driven by the parent TransitionViewModel
    }
}
