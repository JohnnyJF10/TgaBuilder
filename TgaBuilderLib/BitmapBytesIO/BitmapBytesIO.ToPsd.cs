using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Psd;

namespace TgaBuilderLib.BitmapBytesIO;

public partial class BitmapBytesIO
{
    public void ToPsd(IReadableBitmap bitmap)
    {
        if (!bitmap.HasAlpha)
            bitmap = ConvertRGB24ToBGRA32(_mediaFactory.CloneBitmap(bitmap));

        LoadedWidth = bitmap.PixelWidth;
        LoadedHeight = bitmap.PixelHeight;

        ActualDataLength = LoadedWidth * LoadedHeight * 4;
        LoadedBytes = _bytesPool.Rent(ActualDataLength);

        bitmap.CopyPixels(LoadedBytes, LoadedWidth * 4, 0);
    }

    public void WritePsd(string filePath, CancellationToken? cancellationToken = null)
    {
        if (LoadedBytes is null)
            throw new InvalidOperationException("No image data loaded. Please load an image first.");

        var bitmap = _mediaFactory.CreateBitmapFromRaw(
            LoadedWidth, LoadedHeight, hasAlpha: true, LoadedBytes, LoadedWidth * 4);

        var psd = new PsdFile();

        var layerInfo = new PsdLayerInfo(
            bitmap: bitmap,
            rect: new PixelRect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
            name: "Background");

        psd.Save(filePath, bitmap, Enumerable.Empty<PsdLayerInfo>().Append(layerInfo));
    }

    public IWriteableBitmap ConvertRGB24ToBGRA32(IWriteableBitmap sourceBitmap)
    {
        if (sourceBitmap == null)
            throw new ArgumentNullException(nameof(sourceBitmap), "Source bitmap cannot be null.");

        if (sourceBitmap.HasAlpha)
            throw new ArgumentException("Source bitmap must be in RGB24 format.", nameof(sourceBitmap));

        // Create a new IWriteableBitmap with BGRA32 format
        var targetBitmap = _mediaFactory.CreateEmptyBitmap(
            width: sourceBitmap.PixelWidth,
            height: sourceBitmap.PixelHeight,
            hasAlpha: true);

        var targetDirtyRect = new PixelRect(0, 0, targetBitmap.PixelWidth, targetBitmap.PixelHeight);

        // Lock the source and target bitmaps for writing

        using (var sourceLocker = sourceBitmap.GetLocker())
        using (var targetLocker = targetBitmap.GetLocker(targetDirtyRect))
        {
            unsafe
            {
                byte* srcPtr = (byte*)sourceLocker.BackBuffer;
                byte* dstPtr = (byte*)targetLocker.BackBuffer;

                int width = sourceBitmap.PixelWidth;
                int height = sourceBitmap.PixelHeight;
                int srcStride = sourceBitmap.BackBufferStride;
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
