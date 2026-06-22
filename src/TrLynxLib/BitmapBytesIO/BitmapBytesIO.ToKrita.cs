using TrLynxLib.Abstraction;
using TrLynxLib.Krita;

namespace TrLynxLib.BitmapBytesIO;

public partial class BitmapBytesIO
{

    public void ToKrita(IReadableBitmap bitmap)
    {
        LoadedHasAlpha = bitmap.HasAlpha;

        LoadedWidth = bitmap.PixelWidth;
        LoadedHeight = bitmap.PixelHeight;

        int bpp = LoadedHasAlpha ? 4 : 3;

        ActualDataLength = LoadedWidth * LoadedHeight * bpp;
        LoadedBytes = _bytesPool.Rent(ActualDataLength);
        bitmap.CopyPixels(LoadedBytes, LoadedWidth * bpp, 0);
    }

    public void WriteKrita(string filePath, CancellationToken? cancellationToken = null)
    {
        if (LoadedBytes is null)
            throw new ArgumentNullException($"{nameof(LoadedBytes)} is null.");

        _kritaFileService.KraMainDoc.ImageName = Path.GetFileName(filePath);

        _kritaFileService.OutputPath = filePath;

        var layerSource = new LayerSource(bgra: LoadedBytes,
                                          width: LoadedWidth,
                                          height: LoadedHeight,
                                          hasAlpha: LoadedHasAlpha,
                                          name: _kritaFileService.KraMainDoc.ImageName);

        _kritaFileService.LayerSources.Add(layerSource);

        _kritaFileService.WriteFile();

        _kritaFileService.CleanUp();
    }
}
