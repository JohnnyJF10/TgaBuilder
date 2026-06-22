using System.Windows.Input;
using TrLynxLib.Abstraction;
using TrLynxLib.Commands;
using TrLynxLib.Modifications;

namespace TrLynxLib.ViewModel;

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
        ColorOverlayViewModel colorOverlayVM,
        ColorOverrideViewModel colorOverrideVM,
        RetrofierViewModel retrofierVM
        )
    {
        _mediaFactory = mediaFactory;
        _modificationsHelper = modificationsHelper;
        _mainViewModel = mainViewModel;

        BasicVM = basicVM;
        ColorVM = colorVM;
        ColorOverlayVM = colorOverlayVM;
        ColorOverrideVM = colorOverrideVM;
        RetrofierVM = retrofierVM;

        ModificationInVM = BasicVM.ModificationInVM;
        ModificationOutVM = ModificationInVM.ModificationOutVM;

        LoadInput();
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
    public ColorOverrideViewModel ColorOverrideVM { get; set; }
    public RetrofierViewModel RetrofierVM { get; set; }


    public ModificationsInViewModel ModificationInVM { get; set; }
    public ModificationsOutViewModel ModificationOutVM { get; set; }


    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand? _loadImageInCommand;
    private RelayCommand? _loadSecondaryImageCommand;
    private RelayCommand? _applyCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand LoadImageInCommand => _loadImageInCommand
        ??= new RelayCommand(LoadInput);

    public ICommand LoadSecondaryImageCommand => _loadSecondaryImageCommand
        ??= new RelayCommand(() => ColorOverrideVM.LoadSecondaryImage(_mainViewModel.Selection.Presenter));

    // Loads the input texture from the current selection (provisioning the helper
    // buffers for its size) and re-fits the secondary colour-override texture to
    // the new buffer size.
    private void LoadInput()
    {
        ModificationInVM.LoadImageIn(_mainViewModel.Selection.Presenter);
        ColorOverrideVM.OnInputResized();
    }

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
        _modificationsHelper.CleanUp();

        ModificationInVM.ResetImages();
        ModificationOutVM.ResetImages();
        ColorOverrideVM.ResetState();

        _mainViewModel.IsModificationsViewOpen = false;
    }
}
