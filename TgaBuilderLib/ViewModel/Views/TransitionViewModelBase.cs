using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public abstract class TransitionViewModelBase : ViewModelBase
{
    protected TransitionViewModelBase(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        IBitmapOperations bitmapOperations,
        MainViewModel mainViewModel)
    {
        MediaFactory = mediaFactory;
        TransitionHelper = transitionHelper;
        BitmapOperations = bitmapOperations;
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

    protected IBitmapOperations BitmapOperations { get; }

    protected byte[] Pixels1 => _pixels1;
    protected byte[] Pixels2 => _pixels2;

    private CancellationTokenSource? _cts;

    private const int TRANSITIONS_BPP = 4;

    private IWriteableBitmap _image1;
    private IWriteableBitmap _image2;
    private IWriteableBitmap _resultImage;

    private bool _initTextVisible = true;

    private byte[] _pixels1;
    private byte[] _pixels2;

    private TransitionMode _transitionMode = TransitionMode.Top;
    private float _pivotValue = 0.5f;

    private Color _colorSource = new(0, 0, 0, 0);
    private Color _colorTarget = new(0, 0, 0, 0);

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

    public bool InitTextVisible
    {
        get => _initTextVisible;
        set => SetCallerProperty(ref _initTextVisible, value);
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
        set => SetOneWayPropertyTriggerRecalculation(ref _pivotValue, value, RequiresFullAnalysisOnPivotChange);
    }

    public Color ColorSource
    {
        get => _colorSource;
        set => SetCallerProperty(ref _colorSource, value);
    }

    public Color ColorTarget
    {
        get => _colorTarget;
        set => SetCallerProperty(ref _colorTarget, value);
    }

    protected virtual bool RequiresFullAnalysisOnPivotChange => true;

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

    protected void SetOneWayPropertyTriggerRecalculation<T>(
        ref T field,
        T value,
        bool requiresAnalysis = true)
    {
        field = value;
        TriggerRecalculation(requiresAnalysis);
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

            ConfigureTransitionHelper(Image1.PixelWidth, Image1.PixelHeight);

            var resultPixels = await Task.Run(() => CreateMixedPixels(requiresAnalysis), cts.Token);

            if (!cts.Token.IsCancellationRequested)
            {
                var resImage = MediaFactory.CreateBitmapFromRaw(Image1.PixelWidth, Image1.PixelHeight, Image1.HasAlpha, resultPixels, stride: Image1.PixelWidth * TRANSITIONS_BPP);

                ResultImage = MediaFactory.CloneBitmap(resImage);

                OnResultUpdated();
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

    private void LoadImage1()
    {
        Image1 = MainViewModel.Selection.Presenter.HasAlpha 
            ? MediaFactory.CloneBitmap(MainViewModel.Selection.Presenter)
            : BitmapOperations.ConvertRGB24ToBGRA32(MainViewModel.Selection.Presenter);

        _pixels1 = new byte[Image1.PixelWidth * Image1.PixelHeight * TRANSITIONS_BPP];
        Image1.CopyPixels(_pixels1, Image1.PixelWidth * TRANSITIONS_BPP, 0);
        InitTextVisible = false;
    }

    private void LoadImage2()
    {
        Image2 = MainViewModel.Selection.Presenter.HasAlpha
            ? MediaFactory.CloneBitmap(MainViewModel.Selection.Presenter)
            : BitmapOperations.ConvertRGB24ToBGRA32(MainViewModel.Selection.Presenter);
        
        _pixels2 = new byte[Image2.PixelWidth * Image2.PixelHeight * TRANSITIONS_BPP];
        Image2.CopyPixels(_pixels2, Image2.PixelWidth * TRANSITIONS_BPP, 0);
        InitTextVisible = false;
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

        ConfigureTransitionHelper(Image1.PixelWidth, Image1.PixelHeight);

        var resultPixels = CreateMixedPixels(requiresAnalysis: true);

        ResultImage = MediaFactory.CreateEmptyBitmap(Image1.PixelWidth, Image1.PixelHeight, Image1.HasAlpha);
        ResultImage.WritePixels(
            new PixelRect(0, 0, ResultImage.PixelWidth, ResultImage.PixelHeight),
            resultPixels,
            ResultImage.PixelWidth * TRANSITIONS_BPP);

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

    private void ConfigureTransitionHelper(int width, int height)
    {
        TransitionHelper.Width = width;
        TransitionHelper.Height = height;
        TransitionHelper.Stride = width * TRANSITIONS_BPP;

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
