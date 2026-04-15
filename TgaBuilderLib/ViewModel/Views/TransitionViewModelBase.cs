using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public abstract class TransitionViewModelBase : ViewModelBase
{
    protected TransitionViewModelBase(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        MainViewModel mainViewModel)
    {
        MediaFactory = mediaFactory;
        TransitionHelper = transitionHelper;
        MainViewModel = mainViewModel;

        _image1 = MediaFactory.CreateEmptyBitmap(64, 64, true);
        _image2 = MediaFactory.CreateEmptyBitmap(64, 64, true);
        _resultImage = MediaFactory.CreateEmptyBitmap(64, 64, true);

        _pixels1 = new byte[64 * 64 * 4];
        _pixels2 = new byte[64 * 64 * 4];
    }

    protected IMediaFactory MediaFactory { get; }
    protected ITransitionHelper TransitionHelper { get; }
    protected MainViewModel MainViewModel { get; }

    protected byte[] Pixels1 => _pixels1;
    protected byte[] Pixels2 => _pixels2;

    private CancellationTokenSource? _cts;

    private readonly Dictionary<string, TransitionMode> _keyValuePairs = new()
    {
        { nameof(TransistionTopChecked), TransitionMode.Top },
        { nameof(TransistionRightChecked), TransitionMode.Right },
        { nameof(TransistionBottomChecked), TransitionMode.Bottom },
        { nameof(TransistionLeftChecked), TransitionMode.Left },
        { nameof(TransistionDiagonalTopLeftChecked), TransitionMode.DiagonalTopLeft },
        { nameof(TransistionDiagonalTopRightChecked), TransitionMode.DiagonalTopRight }
    };

    private IWriteableBitmap _image1;
    private IWriteableBitmap _image2;
    private IWriteableBitmap _resultImage;

    private byte[] _pixels1;
    private byte[] _pixels2;

    private TransitionMode _transitionMode = TransitionMode.Top;
    private float _pivotValue = 0.5f;

    private RelayCommand? _loadImage1Command;
    private RelayCommand? _loadImage2Command;
    private RelayCommand? _swapImagesCommand;
    private RelayCommand? _mixCommand;
    private RelayCommand? _markFinishedCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand MixCommand => _mixCommand ??= new RelayCommand(Mix);
    public ICommand LoadImage1Command => _loadImage1Command ??= new RelayCommand(LoadImage1);
    public ICommand LoadImage2Command => _loadImage2Command ??= new RelayCommand(LoadImage2);
    public ICommand SwapImagesCommand => _swapImagesCommand ??= new RelayCommand(SwapImages);
    public ICommand MarkFinishedCommand => _markFinishedCommand ??= new RelayCommand(MarkFinished);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);

    public IWriteableBitmap Image1
    {
        get => _image1;
        set => SetCallerProperty(ref _image1, value);
    }

    public IWriteableBitmap Image2
    {
        get => _image2;
        set => SetCallerProperty(ref _image2, value);
    }

    public IWriteableBitmap ResultImage
    {
        get => _resultImage;
        set => SetCallerProperty(ref _resultImage, value);
    }

    public IVisualInvalidator? VisualInvalidator { get; set; }

    public TransitionMode TransitionMode
    {
        get => _transitionMode;
        set => SetPropertyTriggerRecalculation(ref _transitionMode, value);
    }

    public float PivotValue
    {
        get => _pivotValue;
        set => SetPropertyTriggerRecalculation(ref _pivotValue, value, RequiresFullAnalysisOnPivotChange);
    }

    protected virtual bool RequiresFullAnalysisOnPivotChange => true;

    public bool TransistionTopChecked
    {
        get => CheckIfChecked();
        set => ApplySelection(value);
    }

    public bool TransistionRightChecked
    {
        get => CheckIfChecked();
        set => ApplySelection(value);
    }

    public bool TransistionBottomChecked
    {
        get => CheckIfChecked();
        set => ApplySelection(value);
    }

    public bool TransistionLeftChecked
    {
        get => CheckIfChecked();
        set => ApplySelection(value);
    }

    public bool TransistionDiagonalTopLeftChecked
    {
        get => CheckIfChecked();
        set => ApplySelection(value);
    }

    public bool TransistionDiagonalTopRightChecked
    {
        get => CheckIfChecked();
        set => ApplySelection(value);
    }

    protected bool SetCallerPropertyReturn<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            return true;
        }

        return false;
    }

    protected void SetPropertyTriggerRecalculation<T>(
        ref T field,
        T value,
        bool requiresAnalysis = true,
        [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            TriggerRecalculation(requiresAnalysis);
        }
    }

    protected async void TriggerRecalculation(bool requiresAnalysis = true)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        try
        {
            await Task.Delay(10, cts.Token);

            if (!CompareInputSpecs())
                return;

            if (!CompareResultsSpecs())
                return;

            ConfigureTransitionHelper(Image1.PixelWidth, Image1.PixelHeight, Image1.HasAlpha ? 4 : 3);

            var resultPixels = await Task.Run(() => CreateMixedPixels(requiresAnalysis), cts.Token);

            if (!cts.Token.IsCancellationRequested)
            {
                ResultImage.WritePixels(
                    new PixelRect(0, 0, ResultImage.PixelWidth, ResultImage.PixelHeight),
                    resultPixels,
                    ResultImage.PixelWidth * (Image1.HasAlpha ? 4 : 3));

                OnResultUpdated();
                VisualInvalidator?.InvalidateVisual();
            }
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
    }

    protected virtual void OnResultUpdated()
    {
    }

    protected abstract byte[] CreateMixedPixels(bool requiresAnalysis);

    protected abstract void ConfigureTransitionHelperCore();

    private void SetTransitionMode([CallerMemberName] string? propertyName = null)
    {
        if (_keyValuePairs.TryGetValue(propertyName ?? string.Empty, out TransitionMode mode))
            _transitionMode = mode;
    }

    private string? GetSelectedMode()
        => _keyValuePairs.FirstOrDefault(x => x.Value == _transitionMode).Key;

    private bool CheckIfChecked([CallerMemberName] string? propertyName = null)
    {
        if (_keyValuePairs.TryGetValue(propertyName ?? string.Empty, out TransitionMode mode))
            return _transitionMode == mode;

        return false;
    }

    private void ApplySelection(bool value, [CallerMemberName] string? propertyName = null)
    {
        if (!value)
            return;

        string? oldSelected = GetSelectedMode();

        SetTransitionMode(propertyName);
        if (!string.IsNullOrEmpty(oldSelected))
            OnPropertyChanged(oldSelected);

        OnPropertyChanged(propertyName ?? string.Empty);

        TriggerRecalculation();
    }

    private void LoadImage1()
    {
        Image1 = MediaFactory.CloneBitmap(MainViewModel.Selection.Presenter);
        _pixels1 = new byte[Image1.PixelWidth * Image1.PixelHeight * (Image1.HasAlpha ? 4 : 3)];
        Image1.CopyPixels(_pixels1, Image1.PixelWidth * (Image1.HasAlpha ? 4 : 3), 0);
    }

    private void LoadImage2()
    {
        Image2 = MediaFactory.CloneBitmap(MainViewModel.Selection.Presenter);
        _pixels2 = new byte[Image2.PixelWidth * Image2.PixelHeight * (Image2.HasAlpha ? 4 : 3)];
        Image2.CopyPixels(_pixels2, Image2.PixelWidth * (Image2.HasAlpha ? 4 : 3), 0);
    }

    private void SwapImages()
    {
        var tempImage = Image1;
        Image1 = Image2;
        Image2 = tempImage;

        var tempPixels = _pixels1;
        _pixels1 = _pixels2;
        _pixels2 = tempPixels;

        TriggerRecalculation();
    }

    private void Mix()
    {
        if (!CompareInputSpecs())
            return;

        ConfigureTransitionHelper(Image1.PixelWidth, Image1.PixelHeight, Image1.HasAlpha ? 4 : 3);

        var resultPixels = CreateMixedPixels(requiresAnalysis: true);

        ResultImage = MediaFactory.CreateEmptyBitmap(Image1.PixelWidth, Image1.PixelHeight, Image1.HasAlpha);
        ResultImage.WritePixels(
            new PixelRect(0, 0, ResultImage.PixelWidth, ResultImage.PixelHeight),
            resultPixels,
            ResultImage.PixelWidth * (Image1.HasAlpha ? 4 : 3));

        OnResultUpdated();
    }

    private bool CompareInputSpecs()
        => Image1.PixelWidth == Image2.PixelWidth &&
           Image1.PixelHeight == Image2.PixelHeight &&
           Image1.HasAlpha == Image2.HasAlpha;

    private bool CompareResultsSpecs()
        => ResultImage.PixelWidth == Image1.PixelWidth &&
           ResultImage.PixelHeight == Image1.PixelHeight &&
           ResultImage.HasAlpha == Image1.HasAlpha;

    private void ConfigureTransitionHelper(int width, int height, int bpp)
    {
        TransitionHelper.Width = width;
        TransitionHelper.Height = height;
        TransitionHelper.Bpp = bpp;
        TransitionHelper.Stride = width * bpp;

        TransitionHelper.Mode = _transitionMode;
        TransitionHelper.Pivot = PivotValue;

        ConfigureTransitionHelperCore();
    }

    private void MarkFinished()
    {
        TransitionHelper.CleanUp();
        MainViewModel.IsTransitionViewOpen = false;
    }

    private void OK(IView view)
    {
        MainViewModel.Selection.Presenter = MediaFactory.CloneBitmap(ResultImage);
        MarkFinished();
        view.CloseAsync();
    }

    private void Cancel(IView view)
    {
        MarkFinished();
        view.CloseAsync();
    }
}
