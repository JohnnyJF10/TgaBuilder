using System.Runtime.InteropServices;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Modifications;

namespace TgaBuilderLib.ViewModel;

public class ModificationOutViewModel : ThrottledViewModelBase
{
    public ModificationOutViewModel(
    IMediaFactory mediaFactory,
    IModificationsHelper modificationHelper,
    IBitmapOperations bitmapOperations)
    {
        _mediaFactory = mediaFactory;
        _modificationHelper = modificationHelper;
        _bitmapOperations = bitmapOperations;

        _imageOut = _mediaFactory.CreateEmptyBitmap(64, 64, true);

        _modificationHelper.RecalculationCompleted += OnRecalculationCompleted;
    }

    private const int TRANSITIONS_BPP = 4;

    private readonly IMediaFactory _mediaFactory;
    private readonly IModificationsHelper _modificationHelper;
    private readonly IBitmapOperations _bitmapOperations;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private IWriteableBitmap _imageOut;

    public IVisualInvalidator? VisualInvalidator { get; set; }



    public IWriteableBitmap ImageOut
    {
        get => _imageOut;
        set => SetCallerProperty(ref _imageOut, value);
    }



    public void ResetImages()
    {
        _imageOut = _mediaFactory.CreateEmptyBitmap(64, 64, true);
    }



    private void OnRecalculationCompleted(object? sender, EventArgs e)
    {
        byte[] output = _modificationHelper.PixelsOutput;
        int expectedLength = ImageOut.PixelWidth * ImageOut.PixelHeight * TRANSITIONS_BPP;

        // Skip not-yet-provisioned or stale buffers to avoid a size mismatch.
        if (output.Length == 0 || output.Length != expectedLength)
            return;

        using var ResLockedFrameBuffer = ImageOut.GetLocker(requiresRefresh: true);

        Marshal.Copy(
            source: output,
            startIndex: 0,
            destination: ResLockedFrameBuffer.BackBuffer,
            length: output.Length);

        VisualInvalidator?.InvalidateVisual();
    }

    protected override async Task TriggerRecalculation()
    {
        await _modificationHelper.QueueRecalc();
    }
}
