using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Holds shared bitmap/image state for all transition child view models.
/// The window VM and each child hold a reference to this instance.
/// </summary>
public class TransitionPresentersViewModel : ViewModelBase
{
    public TransitionPresentersViewModel(IMediaFactory mediaFactory)
    {
        _mediaFactory = mediaFactory;
        _image1 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _image2 = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _resultImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _pixels1 = new byte[64 * 64 * 4];
        _pixels2 = new byte[64 * 64 * 4];
    }

    private readonly IMediaFactory _mediaFactory;

    private IWriteableBitmap _image1;
    private IWriteableBitmap _image2;
    private IWriteableBitmap _resultImage;
    private IWriteableBitmap? _labelMapImage;
    private IWriteableBitmap? _indicatorMapImage;
    private byte[] _pixels1;
    private byte[] _pixels2;
    private bool _initTextVisible = true;
    private bool _isIndicatorMapVisible;
    private bool _isSmoothMode;

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

    public IWriteableBitmap? IndicatorMapImage
    {
        get => _indicatorMapImage;
        set => SetCallerProperty(ref _indicatorMapImage, value);
    }

    public bool IsIndicatorMapVisible
    {
        get => _isIndicatorMapVisible;
        set => SetCallerProperty(ref _isIndicatorMapVisible, value);
    }

    public bool InitTextVisible
    {
        get => _initTextVisible;
        set => SetCallerProperty(ref _initTextVisible, value);
    }

    public bool IsSmoothMode
    {
        get => _isSmoothMode;
        set => SetCallerProperty(ref _isSmoothMode, value);
    }

    public byte[] Pixels1
    {
        get => _pixels1;
        set => _pixels1 = value;
    }

    public byte[] Pixels2
    {
        get => _pixels2;
        set => _pixels2 = value;
    }

    public IVisualInvalidator? VisualInvalidator { get; set; }
}
