using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;



// =========================================================================
// Unified TransitionViewModel — no base class
// =========================================================================

public class TransitionViewModel : ThrottledViewModelBase
{
    public TransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        IBitmapOperations bitmapOperations,
        AnalysisViewModel analysisViewModel,
        PivotViewModel pivotViewModel,
        EdgeViewModel edgeViewModel,
        ShadowViewModel shadowViewModel,
        UnderfillingViewModel underfillingViewModel,
        MainViewModel mainViewModel)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _bitmapOperations = bitmapOperations;
        _mainViewModel = mainViewModel;

        AnalysisVM = analysisViewModel;
        PivotVM = pivotViewModel;
        EdgeVM = edgeViewModel;
        ShadowVM = shadowViewModel;
        UnderfillingVM = underfillingViewModel;

        TransitionInVM = PivotVM.TransitionInVM;
        TransitionOutVM = TransitionInVM.TransitionOutVM;

        IsBrickMode = true;

        _transitionHelper.EnsureBuffers(64, 64);
    }

    // =====================================================================
    // Infrastructure
    // =====================================================================

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly MainViewModel _mainViewModel;
    private readonly IBitmapOperations _bitmapOperations;

    private const int TRANSITIONS_BPP = 4;

    public TransitionInViewModel TransitionInVM { get; set; }

    public TransitionOutViewModel TransitionOutVM { get; set; }
    public AnalysisViewModel AnalysisVM { get; set; }
    public PivotViewModel PivotVM { get; set; }
    public EdgeViewModel EdgeVM { get; set; }
    public ShadowViewModel ShadowVM { get; set; }
    public UnderfillingViewModel UnderfillingVM { get; set; }

    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand? _loadImage1Command;
    private RelayCommand? _loadImage2Command;

    private RelayCommand? _mixCommand;

    private RelayCommand? _markFinishedCommand;
    private RelayCommand? _applyCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand MixCommand => _mixCommand ??= new RelayCommand(Mix);

    public ICommand LoadImage1Command => _loadImage1Command 
        ??= new RelayCommand(() => TransitionInVM.LoadImage1(_mainViewModel.Selection.Presenter));
    public ICommand LoadImage2Command => _loadImage2Command 
        ??= new RelayCommand(() => TransitionInVM.LoadImage2(_mainViewModel.Selection.Presenter));

    public ICommand MarkFinishedCommand => _markFinishedCommand ??= new RelayCommand(MarkFinished);
    public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(Apply);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);


    // =====================================================================
    // Transition type selection
    // =====================================================================

    public bool IsSmoothMode
    {
         get =>_transitionHelper.TypeOfTransition == TransitionType.Smooth;
         set
         {
             if (value)
             {
                 _transitionHelper.TypeOfTransition = TransitionType.Smooth;
                 OnTransitionTypeChanged();
             }
         }
    }
    public bool IsBrickMode
    { 
        get => _transitionHelper.TypeOfTransition == TransitionType.Bricks;
        set
        {
            if (value)
            {
                _transitionHelper.TypeOfTransition = TransitionType.Bricks;
                OnTransitionTypeChanged();
            }
        }
    }

    public void OnTransitionTypeChanged()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;
        TransitionOutVM.EndMouseInteraction();
        TransitionInVM.EndMouseInteractivity();
        OnPropertyChanged(nameof(IsSmoothMode));
        OnPropertyChanged(nameof(IsBrickMode));

        _ = TriggerRecalculation();
    }

    // =====================================================================
    // Pipeline logic
    // =====================================================================

    private void Mix()
    {
        TransitionInVM.Mix();
    }

    private void MarkFinished()
    {
        TransitionInVM.ResetImages();
        TransitionInVM.EndMouseInteractivity();

        TransitionOutVM.ResetImages();
        TransitionOutVM.EndMouseInteraction();

        _transitionHelper.CleanUp();
        _mainViewModel.IsTransitionViewOpen = false;
    }

    private void Apply()
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(TransitionOutVM.ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
    }

    private void OK(IView view)
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(TransitionOutVM.ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
        MarkFinished();
        view.CloseAsync();
    }

    private void Cancel(IView view)
    {
        MarkFinished();
        view.CloseAsync();
    }

    protected override async Task TriggerRecalculation()
    {
        await _transitionHelper.QueueRecalc();
    }
}
