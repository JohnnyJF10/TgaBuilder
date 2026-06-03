using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

// =========================================================================
// Enum: identifies which transition pipeline the window is operating in
// =========================================================================

public enum TransitionType
{
    Smooth,
    Bricks,
}

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

        TransitionsPresentersVM = PivotVM.TransitionsPresentersVM;
    }

    // =====================================================================
    // Infrastructure
    // =====================================================================

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly MainViewModel _mainViewModel;
    private readonly IBitmapOperations _bitmapOperations;

    private const int TRANSITIONS_BPP = 4;

    public TransitionsPresentersViewModel TransitionsPresentersVM { get; set; }
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
        ??= new RelayCommand(() => TransitionsPresentersVM.LoadImage1(_mainViewModel.Selection.Presenter));
    public ICommand LoadImage2Command => _loadImage2Command 
        ??= new RelayCommand(() => TransitionsPresentersVM.LoadImage2(_mainViewModel.Selection.Presenter));

    public ICommand MarkFinishedCommand => _markFinishedCommand ??= new RelayCommand(MarkFinished);
    public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(Apply);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);


    // =====================================================================
    // Pipeline logic
    // =====================================================================

    private void Mix()
    {
        TransitionsPresentersVM.Mix();
    }



    private void MarkFinished()
    {
        TransitionsPresentersVM.CleanUp();
        _transitionHelper.CleanUp();
        _mainViewModel.IsTransitionViewOpen = false;
    }

    private void Apply()
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(TransitionsPresentersVM.ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
    }

    private void OK(IView view)
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(TransitionsPresentersVM.ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
        MarkFinished();
        view.CloseAsync();
    }

    private void Cancel(IView view)
    {
        MarkFinished();
        view.CloseAsync();
    }


    protected override bool PreProcess()
    {
        return TransitionsPresentersVM.DoPreProcessing();
    }

    protected override async Task Recalculate()
    {
        await TransitionsPresentersVM.DoRecalculation();
    }
}
