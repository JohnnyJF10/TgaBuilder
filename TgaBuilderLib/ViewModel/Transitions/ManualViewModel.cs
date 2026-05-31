using TgaBuilderLib.Transitions;
using TgaBuilderLib.ViewModel.Transitions;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick transition manual draw/erase section.
/// Controls explicit tile visibility draw/erase modes.
/// </summary>
public class TransitionManualViewModel : ThrottledViewModelBase
{
    public TransitionManualViewModel(
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

    private bool _isExplicitTileVisibilityDrawMode;
    private bool _isExplicitTileVisibilityEraseMode;
    private bool _isLabelMapExpanded;

    // =====================================================================
    // Properties
    // =====================================================================

    public bool IsExplicitTileVisibilityDrawMode
    {
        get => _isExplicitTileVisibilityDrawMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityDrawMode, value, nameof(IsExplicitTileVisibilityDrawMode));

            if (!value)
                return;

            _isExplicitTileVisibilityEraseMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityEraseMode));
        }
    }

    public bool IsExplicitTileVisibilityEraseMode
    {
        get => _isExplicitTileVisibilityEraseMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityEraseMode, value, nameof(IsExplicitTileVisibilityEraseMode));
            if (!value)
                return;

            _isExplicitTileVisibilityDrawMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityDrawMode));
        }
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;
        return true;
    }

    protected override void Recalculate()
    {
        // Recalculation is driven by the parent TransitionViewModel
    }
}
