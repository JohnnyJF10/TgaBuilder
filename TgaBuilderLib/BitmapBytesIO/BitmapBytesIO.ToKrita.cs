using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Krita;

namespace TgaBuilderLib.BitmapBytesIO;

public partial class BitmapBytesIO
{

    public void ToKrita(IReadableBitmap bitmap)
    {
        if (!bitmap.HasAlpha)
        {
            bitmap = GetBgra32fromRgb24(_mediaFactory.CloneBitmap(bitmap));
        }

        _lastHadAlpha = bitmap.HasAlpha;

        LoadedWidth = bitmap.PixelWidth;
        LoadedHeight = bitmap.PixelHeight;

        ActualDataLength = LoadedWidth * LoadedHeight * 4;
        LoadedBytes = _bytesPool.Rent(ActualDataLength);
        bitmap.CopyPixels(LoadedBytes, LoadedWidth * 4, 0);
    }

    public void WriteKrita(string filePath, CancellationToken? cancellationToken = null)
    {
        if (LoadedBytes is null)
            throw new ArgumentNullException($"{nameof(LoadedBytes)} is null.");

        _kritaFileService.KraMainDoc.ImageName = Path.GetFileName(filePath);

        _kritaFileService.OutputPath = filePath;

        var layerSource = new LayerSource(LoadedBytes, LoadedWidth, LoadedHeight, _kritaFileService.KraMainDoc.ImageName);

        _kritaFileService.LayerSources.Add(layerSource);

        _kritaFileService.WriteFile();

        _kritaFileService.CleanUp();
    }

    private IWriteableBitmap GetBgra32fromRgb24(IWriteableBitmap rgb24Bmmp)
    {
        if (rgb24Bmmp == null)
            throw new ArgumentNullException(nameof(rgb24Bmmp), "Source bitmap cannot be null.");

        if (rgb24Bmmp.HasAlpha)
            throw new ArgumentException("Source bitmap must be in RGB24 format.", nameof(rgb24Bmmp));

        // Create a new IWriteableBitmap with BGRA32 format
        var targetBitmap = _mediaFactory.CreateEmptyBitmap(
            width: rgb24Bmmp.PixelWidth,
            height: rgb24Bmmp.PixelHeight,
            hasAlpha: true);

        var targetDirtyRect = new PixelRect(0, 0, targetBitmap.PixelWidth, targetBitmap.PixelHeight);

        // Lock the source and target bitmaps for writing

        using (var sourceLocker = rgb24Bmmp.GetLocker())
        using (var targetLocker = targetBitmap.GetLocker(targetDirtyRect))
        {
            unsafe
            {
                byte* srcPtr = (byte*)sourceLocker.BackBuffer;
                byte* dstPtr = (byte*)targetLocker.BackBuffer;

                int width = rgb24Bmmp.PixelWidth;
                int height = rgb24Bmmp.PixelHeight;
                int srcStride = rgb24Bmmp.BackBufferStride;
                int dstStride = targetBitmap.BackBufferStride;

                int dstIdx = 0, srcIdx = 0;

                byte r, g, b;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        srcIdx = (y * srcStride) + (x * 3);

                        // Read RGB values from the source bitmap
                        r = srcPtr[srcIdx];
                        g = srcPtr[srcIdx + 1];
                        b = srcPtr[srcIdx + 2];

                        dstIdx = (y * dstStride) + (x * 4);

                        if ((r, g, b) != (255, 0, 255)) // Write BGRA values to the target bitmap if not magenta
                        {
                            dstPtr[dstIdx] = b;   // B
                            dstPtr[dstIdx + 1] = g;   // G
                            dstPtr[dstIdx + 2] = r;   // R
                            dstPtr[dstIdx + 3] = 255; // A (fully opaque)
                        }
                    }
                }
            }
        }


        return targetBitmap;
    }
}
