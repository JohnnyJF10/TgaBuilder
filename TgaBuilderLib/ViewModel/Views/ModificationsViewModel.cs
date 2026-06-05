using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel;

// =========================================================================
// ModificationsViewModel — flat standalone class
// =========================================================================

public class ModificationsViewModel : ViewModelBase
{
    public ModificationsViewModel(
        IMediaFactory mediaFactory,
        IModificationsHelper modificationsHelper,
        MainViewModel mainViewModel,
        
        BasicViewModel basicVM,
        ColorViewModel colorVM,
        ColorOverlayViewModel colorOverlayVM
        )
    {
        _mediaFactory = mediaFactory;
        _modificationsHelper = modificationsHelper;
        _mainViewModel = mainViewModel;

        BasicVM = basicVM;
        ColorVM = colorVM;
        ColorOverlayVM = colorOverlayVM;

        ModificationInVM = BasicVM.ModificationInVM;
        ModificationOutVM = ModificationInVM.ModificationOutVM;
    }

    // =====================================================================
    // Infrastructure
    // =====================================================================

    private readonly IMediaFactory _mediaFactory;
    private readonly IModificationsHelper _modificationsHelper;
    private readonly MainViewModel _mainViewModel;


    // =====================================================================
    // View Model Children
    // =====================================================================

    public BasicViewModel BasicVM { get; set; } 
    public ColorViewModel ColorVM { get; set; }
    public ColorOverlayViewModel ColorOverlayVM { get; set; }


    public ModificationInViewModel ModificationInVM { get; set; }
    public ModificationOutViewModel ModificationOutVM { get; set; }


    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand? _applyCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;


    public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(Apply);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);






    // =====================================================================
    // Actions
    // =====================================================================
 

    private void Apply()
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(ModificationOutVM.ImageOut);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
    }

    private void OK(IView view)
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(ModificationOutVM.ImageOut);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
        MarkFinished();
        view.CloseAsync();
    }

    private void Cancel(IView view)
    {
        MarkFinished();
        view.CloseAsync();
    }

    public void MarkFinished()
    {
        _mainViewModel.IsModificationsViewOpen = false;
    }
}
