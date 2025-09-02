using Pfim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        public void FromPfim(
            string filePath,
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            using var stream = File.OpenRead(filePath);
            using var image = Pfimage.FromStream(stream);

            LoadedHasAlpha = image.Format == ImageFormat.Rgba32;

            int bytesPerPixel = LoadedHasAlpha ? 4 : 3;

            int originalWidth = image.Width;
            int originalHeight = image.Height;

            LoadedWidth = CalculatePaddedWidth(originalWidth, mode);
            LoadedHeight = CalculatePaddedHeight(originalHeight, mode);

            LoadedStride = LoadedWidth * bytesPerPixel;

            LoadedBytes = RentBlackPixelBuffer(
                LoadedWidth,
                LoadedHeight,
                LoadedHasAlpha);

            for (int y = 0; y < originalHeight; y++)
            {
                int srcOffset = y * image.Stride;
                int dstOffset = y * LoadedStride;

                for (int x = 0; x < originalWidth; x++)
                {
                    cancellationToken?.ThrowIfCancellationRequested();

                    int srcIndex = srcOffset + x * image.BitsPerPixel / 8;
                    int dstIndex = dstOffset + x * bytesPerPixel;

                    if (LoadedHasAlpha)
                    {
                        LoadedBytes[dstIndex + 0] = image.Data[srcIndex + 0]; // B
                        LoadedBytes[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                        LoadedBytes[dstIndex + 2] = image.Data[srcIndex + 2]; // R
                        LoadedBytes[dstIndex + 3] = image.Data[srcIndex + 3]; // A
                    }
                    else
                    {
                        LoadedBytes[dstIndex + 0] = image.Data[srcIndex + 2]; // R
                        LoadedBytes[dstIndex + 1] = image.Data[srcIndex + 1]; // G
                        LoadedBytes[dstIndex + 2] = image.Data[srcIndex + 0]; // B
                    }
                }
            }
        }
    }
}
