using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Enums;
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
        IBitmapOperations bitmapOperations,
        MainViewModel mainViewModel)
    {
        _mediaFactory = mediaFactory;
        _modificationsHelper = modificationsHelper;
        _bitmapOperations = bitmapOperations;
        _mainViewModel = mainViewModel;

        _inputImage = _mediaFactory.CreateEmptyBitmap(42, 42, true);
        _resultImage = _mediaFactory.CreateEmptyBitmap(42, 42, true);
        _inputPixels = new byte[42];

        LoadInputImage();
    }

    // =====================================================================
    // Infrastructure
    // =====================================================================

    private readonly IMediaFactory _mediaFactory;
    private readonly IModificationsHelper _modificationsHelper;
    private readonly IBitmapOperations _bitmapOperations;
    private readonly MainViewModel _mainViewModel;

    private const int BPP = 4;

    private readonly object _recalcLock = new();
    private bool _recalcRunning;
    private bool _recalcPending;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private IWriteableBitmap _inputImage;
    private IWriteableBitmap _resultImage;
    private byte[] _inputPixels;
    private bool _initTextVisible = true;

    public IWriteableBitmap InputImage
    {
        get => _inputImage;
        set => SetCallerProperty(ref _inputImage, value);
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

    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand? _loadInputImageCommand;
    private RelayCommand? _applyCommand;
    private RelayCommand<IView>? _cancelCommand;
    private RelayCommand<IView>? _oKCommand;

    public ICommand LoadInputImageCommand => _loadInputImageCommand ??= new RelayCommand(LoadInputImage);
    public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(Apply);
    public ICommand CancelCommand => _cancelCommand ??= new RelayCommand<IView>(Cancel);
    public ICommand OKCommand => _oKCommand ??= new RelayCommand<IView>(OK);

    // =====================================================================
    // Basic adjustments
    // Exposure:    -5 .. +5 (stops)
    // Brightness:  -1 .. +1
    // Contrast:    -1 .. +1
    // Highlights:  -1 .. +1
    // Shadows:     -1 .. +1
    // Whites:      -1 .. +1
    // Blacks:      -1 .. +1
    // =====================================================================

    private float _exposure = 0f;
    private float _brightness = 0f;
    private float _contrast = 0f;
    private float _highlights = 0f;
    private float _shadows = 0f;
    private float _whites = 0f;
    private float _blacks = 0f;

    public float Exposure
    {
        get => _exposure;
        set => SetPropertyTriggerRecalculation(ref _exposure, value);
    }

    public float Brightness
    {
        get => _brightness;
        set => SetPropertyTriggerRecalculation(ref _brightness, value);
    }

    public float Contrast
    {
        get => _contrast;
        set => SetPropertyTriggerRecalculation(ref _contrast, value);
    }

    public float Highlights
    {
        get => _highlights;
        set => SetPropertyTriggerRecalculation(ref _highlights, value);
    }

    public float Shadows
    {
        get => _shadows;
        set => SetPropertyTriggerRecalculation(ref _shadows, value);
    }

    public float Whites
    {
        get => _whites;
        set => SetPropertyTriggerRecalculation(ref _whites, value);
    }

    public float Blacks
    {
        get => _blacks;
        set => SetPropertyTriggerRecalculation(ref _blacks, value);
    }

    // =====================================================================
    // Color adjustments
    // Saturation:   -1 .. +1
    // Vibrance:     -1 .. +1
    // Hue:          -180 .. +180 (degrees)
    // Temperature:  -1 .. +1
    // Tint:         -1 .. +1
    // =====================================================================

    private float _saturation = 0f;
    private float _vibrance = 0f;
    private float _hue = 0f;
    private float _temperature = 0f;
    private float _tint = 0f;
    private Color _colorOverlay = new(0, 0, 0, 0);
    private Color _colorTarget = new(0, 0, 0, 0);
    private float _colorOverlayAmount = 0f;
    private int _selectedColorOverlayMixModeIndex = (int)ColorOverlayMixMode.OklabChroma;
    private float _colorOverlaySoftLightStrength = 1f;
    private float _colorOverlayLumaPreservation = 1f;
    private float _colorOverlayChromaBoost = 1f;

    public float Saturation
    {
        get => _saturation;
        set => SetPropertyTriggerRecalculation(ref _saturation, value);
    }

    public float Vibrance
    {
        get => _vibrance;
        set => SetPropertyTriggerRecalculation(ref _vibrance, value);
    }

    public float Hue
    {
        get => _hue;
        set => SetPropertyTriggerRecalculation(ref _hue, value);
    }

    public float Temperature
    {
        get => _temperature;
        set => SetPropertyTriggerRecalculation(ref _temperature, value);
    }

    public float Tint
    {
        get => _tint;
        set => SetPropertyTriggerRecalculation(ref _tint, value);
    }

    public Color ColorOverlay
    {
        get => _colorOverlay;
        set => SetPropertyTriggerRecalculation(ref _colorOverlay, value);
    }

    public Color ColorTarget
    {
        get => _colorTarget;
        set => SetPropertyTriggerRecalculation(ref _colorTarget, value);
    }

    public float ColorOverlayAmount
    {
        get => _colorOverlayAmount;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_colorOverlayAmount - clamped) > float.Epsilon)
            {
                _colorOverlayAmount = clamped;
                OnPropertyChanged(nameof(ColorOverlayAmount));
                OnPropertyChanged(nameof(ColorOverlayBlendPercent));
                _ = TriggerRecalculation();
            }
        }
    }

    public float ColorOverlayBlendPercent
    {
        get => _colorOverlayAmount * 100f;
        set => ColorOverlayAmount = Math.Clamp(value, 0f, 100f) / 100f;
    }

    public int SelectedColorOverlayMixModeIndex
    {
        get => _selectedColorOverlayMixModeIndex;
        set
        {
            if (_selectedColorOverlayMixModeIndex != value)
            {
                _selectedColorOverlayMixModeIndex = value;
                OnPropertyChanged(nameof(SelectedColorOverlayMixModeIndex));
                OnPropertyChanged(nameof(IsColorOverlaySoftLightMode));
                OnPropertyChanged(nameof(IsColorOverlayOklabMode));
                _ = TriggerRecalculation();
            }
        }
    }

    public float ColorOverlaySoftLightStrength
    {
        get => _colorOverlaySoftLightStrength;
        set => SetPropertyTriggerRecalculation(ref _colorOverlaySoftLightStrength, value);
    }

    public float ColorOverlayLumaPreservation
    {
        get => _colorOverlayLumaPreservation;
        set => SetPropertyTriggerRecalculation(ref _colorOverlayLumaPreservation, value);
    }

    public float ColorOverlayChromaBoost
    {
        get => _colorOverlayChromaBoost;
        set => SetPropertyTriggerRecalculation(ref _colorOverlayChromaBoost, value);
    }

    public bool IsColorOverlaySoftLightMode
        => SelectedColorOverlayMixModeIndex == (int)ColorOverlayMixMode.SoftLight;

    public bool IsColorOverlayOklabMode
        => SelectedColorOverlayMixModeIndex == (int)ColorOverlayMixMode.OklabChroma;

    // =====================================================================
    // Actions
    // =====================================================================

    private void LoadInputImage()
    {
        InputImage = _mainViewModel.Selection.Presenter.HasAlpha
            ? _mediaFactory.CloneBitmap(_mainViewModel.Selection.Presenter)
            : _bitmapOperations.ConvertRGB24ToBGRA32(_mainViewModel.Selection.Presenter);

        _inputPixels = new byte[InputImage.PixelWidth * InputImage.PixelHeight * BPP];
        InputImage.CopyPixels(_inputPixels, InputImage.PixelWidth * BPP, 0);
        InitTextVisible = false;

        _ = TriggerRecalculation();
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
        _mainViewModel.IsModificationsViewOpen = false;
    }

    // =====================================================================
    // Recalculation pipeline
    // =====================================================================

    private void ConfigureHelper()
    {
        _modificationsHelper.Width = InputImage.PixelWidth;
        _modificationsHelper.Height = InputImage.PixelHeight;

        _modificationsHelper.Exposure = _exposure;
        _modificationsHelper.Brightness = _brightness;
        _modificationsHelper.Contrast = _contrast;
        _modificationsHelper.Highlights = _highlights;
        _modificationsHelper.Shadows = _shadows;
        _modificationsHelper.Whites = _whites;
        _modificationsHelper.Blacks = _blacks;

        _modificationsHelper.Saturation = _saturation;
        _modificationsHelper.Vibrance = _vibrance;
        _modificationsHelper.Hue = _hue;
        _modificationsHelper.Temperature = _temperature;
        _modificationsHelper.Tint = _tint;

        _modificationsHelper.ColorOverlay = _colorOverlay;
        _modificationsHelper.ColorTarget = _colorTarget;
        _modificationsHelper.ColorOverlayAmount = _colorOverlayAmount;
        _modificationsHelper.ColorOverlayMixMode = (ColorOverlayMixMode)Math.Clamp(
            _selectedColorOverlayMixModeIndex,
            (int)ColorOverlayMixMode.Linear,
            (int)ColorOverlayMixMode.OklabChroma);
        _modificationsHelper.ColorOverlaySoftLightStrength = _colorOverlaySoftLightStrength;
        _modificationsHelper.ColorOverlayLumaPreservation = _colorOverlayLumaPreservation;
        _modificationsHelper.ColorOverlayChromaBoost = _colorOverlayChromaBoost;
    }

    private async Task TriggerRecalculation()
    {
        lock (_recalcLock)
        {
            if (_recalcRunning)
            {
                _recalcPending = true;
                return;
            }

            _recalcRunning = true;
        }

        try
        {
            do
            {
                lock (_recalcLock)
                {
                    _recalcPending = false;
                }

                if (_inputPixels.Length == 0 || InputImage.PixelWidth == 0)
                    return;

                int width = InputImage.PixelWidth;
                int height = InputImage.PixelHeight;

                await Task.Delay(50);

                ConfigureHelper();

                byte[] inputCopy = new byte[_inputPixels.Length];
                Array.Copy(_inputPixels, inputCopy, _inputPixels.Length);

                byte[] resultPixels = await Task.Run(
                    () => _modificationsHelper.Apply(inputCopy));

                var resImage = _mediaFactory.CreateBitmapFromRaw(
                    width,
                    height,
                    hasAlpha: true,
                    resultPixels,
                    stride: width * BPP);

                ResultImage = _mediaFactory.CloneBitmap(resImage);
            }
            while (_recalcPending);
        }
        finally
        {
            lock (_recalcLock)
            {
                _recalcRunning = false;
            }
        }
    }

    private void SetPropertyTriggerRecalculation<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            _ = TriggerRecalculation();
        }
    }
}
