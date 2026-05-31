using System.ComponentModel;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Modifications;
using TgaBuilderLib.ViewModel.Modifications;

namespace TgaBuilderLib.ViewModel;

public class ModificationsViewModel : ViewModelBase
{
    public ModificationsViewModel(
        IMediaFactory mediaFactory,
        IModificationsHelper modificationsHelper,
        IBitmapOperations bitmapOperations,
        MainViewModel mainViewModel,
        ModificationsPresentersViewModel presenters,
        ModificationsBasicViewModel basic,
        ModificationsColorViewModel color,
        ModificationsColorOverlayViewModel colorOverlay)
    {
        _mediaFactory = mediaFactory;
        _modificationsHelper = modificationsHelper;
        _bitmapOperations = bitmapOperations;
        _mainViewModel = mainViewModel;

        Presenters = presenters;
        BasicVM = basic;
        ColorVM = color;
        ColorOverlayVM = colorOverlay;

        Presenters.PropertyChanged += OnPresentersPropertyChanged;

        LoadInputImage();
    }

    private readonly IMediaFactory _mediaFactory;
    private readonly IModificationsHelper _modificationsHelper;
    private readonly IBitmapOperations _bitmapOperations;
    private readonly MainViewModel _mainViewModel;

    public ModificationsPresentersViewModel Presenters { get; }
    public ModificationsBasicViewModel BasicVM { get; }
    public ModificationsColorViewModel ColorVM { get; }
    public ModificationsColorOverlayViewModel ColorOverlayVM { get; }

    private const int BPP = 4;

    public IWriteableBitmap InputImage
    {
        get => Presenters.InputImage;
        set => Presenters.InputImage = value;
    }

    public IWriteableBitmap ResultImage
    {
        get => Presenters.ResultImage;
        set => Presenters.ResultImage = value;
    }

    public bool InitTextVisible
    {
        get => Presenters.InitTextVisible;
        set => Presenters.InitTextVisible = value;
    }

    private RelayCommand? _loadInputImageCommand;
    private RelayCommand<(int X, int Y)>? _mouseOverInputCommand;
    private RelayCommand? _applyCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand LoadInputImageCommand => _loadInputImageCommand ??= new RelayCommand(LoadInputImage);
    public ICommand MouseOverInputCommand => _mouseOverInputCommand
        ??= new RelayCommand<(int X, int Y)>(args => MouseOverInput(args.X, args.Y));
    public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(Apply);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);

    private void LoadInputImage()
    {
        Presenters.InputImage = _mainViewModel.Selection.Presenter.HasAlpha
            ? _mediaFactory.CloneBitmap(_mainViewModel.Selection.Presenter)
            : _bitmapOperations.ConvertRGB24ToBGRA32(_mainViewModel.Selection.Presenter);

        Presenters.InputPixels = new byte[Presenters.InputImage.PixelWidth * Presenters.InputImage.PixelHeight * BPP];
        Presenters.InputImage.CopyPixels(Presenters.InputPixels, Presenters.InputImage.PixelWidth * BPP, 0);
        Presenters.InitTextVisible = false;

        BasicVM.RequestRecalculation();
    }

    private void MouseOverInput(int x, int y)
    {
        if (!ColorOverlayVM.IsColorOverlayEyedropperMode)
            return;

        ColorOverlayVM.ColorOverlay = _bitmapOperations.GetPixelBrush(InputImage, x, y);
    }

    private void Apply()
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(ResultImage);
        _mainViewModel.SwitchToDestinationPlacingModeCommand.Execute(null);
    }

    private void OK(IView view)
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(ResultImage);
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
        Presenters.PropertyChanged -= OnPresentersPropertyChanged;
        _modificationsHelper.CleanUp();
        _mainViewModel.IsModificationsViewOpen = false;
    }

    private void OnPresentersPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ModificationsPresentersViewModel.InputImage):
                OnPropertyChanged(nameof(InputImage));
                break;
            case nameof(ModificationsPresentersViewModel.ResultImage):
                OnPropertyChanged(nameof(ResultImage));
                break;
            case nameof(ModificationsPresentersViewModel.InitTextVisible):
                OnPropertyChanged(nameof(InitTextVisible));
                break;
        }
    }
}
