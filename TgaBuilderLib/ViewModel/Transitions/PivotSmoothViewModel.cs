using TgaBuilderLib.Transitions;
using TgaBuilderLib.ViewModel.Transitions;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Smooth transition pivot/blend section.
/// Controls blend hardness, pivot, widening, shift.
/// </summary>
public class TransitionPivotSmoothViewModel : ThrottledViewModelBase
{
    public TransitionPivotSmoothViewModel(
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
    private float _blendHardnessValue = 0.5f;

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

    public float BlendHardnessValue
    {
        get => _blendHardnessValue;
        set => SetPropertyTriggerRecalculation(ref _blendHardnessValue, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.Mode = _selectedTransitionMode;
        _transitionHelper.Pivot = _pivotValue;
        _transitionHelper.Widening = _wideningValue;
        _transitionHelper.Shift = _shiftValue;
        _transitionHelper.Hardness = _blendHardnessValue;
        return true;
    }

    protected override void Recalculate()
    {
        // Recalculation is driven by the parent TransitionViewModel
    }
}
