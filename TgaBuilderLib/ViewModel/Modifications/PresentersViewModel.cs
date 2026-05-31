using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.ViewModel.Modifications;

/// <summary>
/// Holds shared bitmap/image state for all modification child view models.
/// The window VM and each child hold a reference to this instance.
/// </summary>
public class ModificationsPresentersViewModel : ViewModelBase
{
    public ModificationsPresentersViewModel(IMediaFactory mediaFactory)
    {
        _mediaFactory = mediaFactory;
        _inputImage = _mediaFactory.CreateEmptyBitmap(42, 42, true);
        _resultImage = _mediaFactory.CreateEmptyBitmap(42, 42, true);
        _inputPixels = new byte[42];
    }

    private readonly IMediaFactory _mediaFactory;

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

    public byte[] InputPixels
    {
        get => _inputPixels;
        set => _inputPixels = value;
    }
}
