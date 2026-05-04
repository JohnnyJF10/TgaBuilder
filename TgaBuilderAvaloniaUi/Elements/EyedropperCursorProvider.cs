using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderAvaloniaUi.Elements;

public static class EyedropperCursorProvider
{
    static EyedropperCursorProvider()
    {
        EyedropperCursor = LoadCursorFromCurFile(new Uri("avares://TgaBuilderAvaloniaUi/Resources/eyedropper.cur"));
    }

    public static readonly Cursor EyedropperCursor;

    private static Cursor LoadCursorFromCurFile(Uri resourceUri)
    {
        try
        {
            using var stream = AssetLoader.Open(resourceUri, null);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var data = ms.ToArray();

            // CUR header: 0-1 reserved, 2-3 type (2=cursor), 4-5 image count
            // Directory entry at offset 6: width(1), height(1), colorCount(1), reserved(1),
            //   hotspotX(2), hotspotY(2), dataSize(4), dataOffset(4)
            int hotspotX = BitConverter.ToUInt16(data, 10);
            int hotspotY = BitConverter.ToUInt16(data, 12);
            int dataOffset = BitConverter.ToInt32(data, 18);

            // BITMAPINFOHEADER at dataOffset
            int width = BitConverter.ToInt32(data, dataOffset + 4);
            int height = BitConverter.ToInt32(data, dataOffset + 8) / 2; // Halved (AND mask doubles it)
            int bpp = BitConverter.ToUInt16(data, dataOffset + 14);

            if (bpp != 32)
                return new Cursor(StandardCursorType.Hand);

            int pixelDataOffset = dataOffset + 40; // After BITMAPINFOHEADER
            int stride = width * 4;

            var wb = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using (var fb = wb.Lock())
            {
                for (int y = 0; y < height; y++)
                {
                    int srcRow = height - 1 - y; // DIB is stored bottom-up
                    int srcOffset = pixelDataOffset + srcRow * stride;
                    int dstOffset = y * fb.RowBytes;
                    Marshal.Copy(data, srcOffset, fb.Address + dstOffset, stride);
                }
            }

            return new Cursor(wb, new PixelPoint(hotspotX, hotspotY));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load custom cursor: {ex.Message}");
            return new Cursor(StandardCursorType.Hand);
        }
    }
}
