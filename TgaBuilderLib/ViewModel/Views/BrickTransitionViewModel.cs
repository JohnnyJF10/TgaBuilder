using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class BrickTransitionViewModel : ViewModelBase
{
    public BrickTransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        MainViewModel mainViewModel)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _mainViewModel = mainViewModel;

        _image1 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _image2 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _resultImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);

        _pixels1 = new byte[64 * 64 * 4];
        _pixels2 = new byte[64 * 64 * 4];
    }

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly MainViewModel _mainViewModel;

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
    private IWriteableBitmap? _labelMapImage;

    private byte[] _pixels1;
    private byte[] _pixels2;

    private TransitionMode _transitionMode = TransitionMode.Top;
    private float _pivotValue = 0.5f;
    private int _markerRadius = 3;
    private int _expectedRegionCount = -1;
    private bool _useExpectedRegionCount;
    private bool _reversePivot;
    private bool _isLabelMapExpanded;

    private RelayCommand? _loadImage1Command;
    private RelayCommand? _loadImage2Command;
    private RelayCommand? _swapImagesCommand;
    private RelayCommand? _mixCommand;
    private RelayCommand? _markFinishedCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand MixCommand => _mixCommand
        ??= new RelayCommand(Mix);
    public ICommand LoadImage1Command => _loadImage1Command
        ??= new RelayCommand(LoadImage1);
    public ICommand LoadImage2Command => _loadImage2Command
        ??= new RelayCommand(LoadImage2);
    public ICommand SwapImagesCommand => _swapImagesCommand
        ??= new RelayCommand(SwapImages);
    public ICommand MarkFinishedCommand => _markFinishedCommand
        ??= new RelayCommand(MarkFinished);
    public ICommand CancelCommand => _cancelCommand
        ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand
        ??= new RelayCommand<IView>(OK);

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

    public IWriteableBitmap? LabelMapImage
    {
        get => _labelMapImage;
        set => SetCallerProperty(ref _labelMapImage, value);
    }

    public TransitionMode TransitionMode
    {
        get => _transitionMode;
        set => SetPropertyTriggerRecalculation(ref _transitionMode, value);
    }

    public float PivotValue
    {
        get => _pivotValue;
        set => SetPivotTriggerRecalculation(ref _pivotValue, value);
    }

    public int MarkerRadius
    {
        get => _markerRadius;
        set => SetPropertyTriggerRecalculation(ref _markerRadius, value);
    }

    public int ExpectedRegionCount
    {
        get => _expectedRegionCount;
        set => SetPropertyTriggerRecalculation(ref _expectedRegionCount, value);
    }

    public bool UseExpectedRegionCount
    {
        get => _useExpectedRegionCount;
        set
        {
            if (SetCallerPropertyReturn(ref _useExpectedRegionCount, value))
            {
                if (!value)
                    ExpectedRegionCount = -1;
                else if (_expectedRegionCount < 0)
                    ExpectedRegionCount = 4;
            }
        }
    }

    public bool ReversePivot
    {
        get => _reversePivot;
        set => SetPropertyTriggerRecalculation(ref _reversePivot, value);
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

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

    private void SetTransitionMode([CallerMemberName] string? propertyName = null)
    {
        if (_keyValuePairs.TryGetValue(propertyName ?? "", out TransitionMode lMode))
            _transitionMode = lMode;
    }

    private string? GetSelectedMode()
        => _keyValuePairs.FirstOrDefault(x => x.Value == _transitionMode).Key;

    private bool CheckIfChecked([CallerMemberName] string? propertyName = null)
    {
        if (_keyValuePairs.TryGetValue(propertyName ?? "", out TransitionMode lMode))
            return _transitionMode == lMode;
        return false;
    }

    private void ApplySelection(bool value, [CallerMemberName] string? propertyName = null)
    {
        if (!value)
            return;

        string? oldSelected = GetSelectedMode();

        SetTransitionMode(propertyName);
        if (!String.IsNullOrEmpty(oldSelected))
            OnPropertyChanged(oldSelected);

        OnPropertyChanged(propertyName ?? "");

        TriggerRecalculation();
    }

    private void LoadImage1()
    {
        Image1 = _mediaFactory.CloneBitmap(_mainViewModel.Selection.Presenter);
        _pixels1 = new byte[Image1.PixelWidth * Image1.PixelHeight * (Image1.HasAlpha ? 4 : 3)];
        Image1.CopyPixels(_pixels1, Image1.PixelWidth * (Image1.HasAlpha ? 4 : 3), 0);
    }

    private void LoadImage2()
    {
        Image2 = _mediaFactory.CloneBitmap(_mainViewModel.Selection.Presenter);
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

        ConfigureTransitionHelper(
            Image1.PixelWidth,
            Image1.PixelHeight,
            Image1.HasAlpha ? 4 : 3);

        _transitionHelper.AnalyzeTilesWatershed(_pixels1);

        var resPixels = _transitionHelper.MixSmartTilesPixels(
            _pixels1,
            _pixels2);

        ResultImage = _mediaFactory.CreateEmptyBitmap(
            Image1.PixelWidth,
            Image1.PixelHeight,
            Image1.HasAlpha);

        ResultImage.WritePixels(
            new PixelRect(0, 0, ResultImage.PixelWidth, ResultImage.PixelHeight),
            resPixels,
            ResultImage.PixelWidth * (Image1.HasAlpha ? 4 : 3));

        UpdateLabelMapImage();
    }

    private async void TriggerRecalculation(bool requiresAnalysis = true)
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

            ConfigureTransitionHelper(
                Image1.PixelWidth,
                Image1.PixelHeight,
                Image1.HasAlpha ? 4 : 3);

            var resultPixels = await Task.Run(() =>
            {
                if (requiresAnalysis || _transitionHelper.LastAnalysisMap == Array.Empty<byte>())
                    _transitionHelper.AnalyzeTilesWatershed(_pixels1);

                return _transitionHelper.MixSmartTilesPixels(
                    _pixels1,
                    _pixels2);
            });

            if (!cts.Token.IsCancellationRequested)
            {
                ResultImage.WritePixels(
                    new PixelRect(0, 0, ResultImage.PixelWidth, ResultImage.PixelHeight),
                    resultPixels,
                    ResultImage.PixelWidth * (Image1.HasAlpha ? 4 : 3));

                UpdateLabelMapImage();
            }
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
    }


    private void UpdateLabelMapImage()
    {
        byte[] mapData = _transitionHelper.LastAnalysisMap;
        int mapW = _transitionHelper.LastAnalysisWidth;
        int mapH = _transitionHelper.LastAnalysisHeight;

        if (mapData.Length == 0 || mapW == 0 || mapH == 0)
            return;

        var labelBmp = _mediaFactory.CreateEmptyBitmap(mapW, mapH, true);
        labelBmp.WritePixels(
            new PixelRect(0, 0, mapW, mapH),
            mapData,
            mapW * 4);

        LabelMapImage = labelBmp;
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
        _transitionHelper.Width = width;
        _transitionHelper.Height = height;
        _transitionHelper.Bpp = bpp;
        _transitionHelper.Stride = width * bpp;

        _transitionHelper.Mode = _transitionMode;
        _transitionHelper.Pivot = PivotValue;
        _transitionHelper.ReversePivot = ReversePivot;
        _transitionHelper.MarkerRadius = MarkerRadius;
        _transitionHelper.ExpectedRegionCount = ExpectedRegionCount;
    }

    private void SetPivotTriggerRecalculation<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? "");
            TriggerRecalculation(false);
        }
    }

    protected void SetPropertyTriggerRecalculation<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? "");
            TriggerRecalculation();
        }
    }

    protected bool SetCallerPropertyReturn<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? "");
            return true;
        }
        return false;
    }

    private void MarkFinished()
    {
        _transitionHelper.CleanUp();
        _mainViewModel.IsTransitionViewOpen = false;
    }
    private void OK(IView view)
    {
        _mainViewModel.Selection.Presenter = _mediaFactory.CloneBitmap(ResultImage);
        MarkFinished();

        view.CloseAsync();
    }

    private void Cancel(IView view)
    {
        MarkFinished();

        view.CloseAsync();
    }
}

