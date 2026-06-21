using System.IO.Compression;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapBytesIO;

public partial class BitmapBytesIO
{
    public void FromKrita(string kraFilePath,
                          ResizeMode mode = ResizeMode.SourceResize,
                          CancellationToken? cancellationToken = null)
    {
        if (string.IsNullOrWhiteSpace(kraFilePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(kraFilePath));

        if (!File.Exists(kraFilePath))
            throw new FileNotFoundException($"The Krita file could not be found at: {kraFilePath}");

        IReadableBitmap sourceBitmap;

        // 2. Open the .kra file as a ZIP archive
        using (ZipArchive archive = ZipFile.OpenRead(kraFilePath))
        {
            // 3. Locate Krita's pre-flattened layer cache
            ZipArchiveEntry? mergedImageEntry = archive.GetEntry("mergedimage.png");

            if (mergedImageEntry is null)
                throw new FileNotFoundException(
                    "The 'mergedimage.png' file was not found inside the .kra archive.");

            // 4. Extract the file to a byte array
            using var entryStream = mergedImageEntry.Open();
            using var ms = new MemoryStream();

            entryStream.CopyTo(ms);

            ms.Position = 0;

            sourceBitmap = _mediaFactory.LoadReadableBitmap(ms);
        }

        LoadedHasAlpha = sourceBitmap.HasAlpha;

        int originalWidth = sourceBitmap.PixelWidth;
        int originalHeight = sourceBitmap.PixelHeight;

        LoadedWidth = CalculatePaddedWidth(originalWidth, mode);
        LoadedHeight = CalculatePaddedHeight(originalHeight, mode);

        int bytesPerPixel = LoadedHasAlpha ? 4 : 3;
        LoadedStride = LoadedWidth * bytesPerPixel;
        LoadedBytes = RentBlackPixelBuffer(LoadedWidth, LoadedHeight, LoadedHasAlpha);

        sourceBitmap.CopyPixels(LoadedBytes, LoadedStride, 0);
    }
}