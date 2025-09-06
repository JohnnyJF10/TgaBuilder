using Pfim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        public void FromUsual(
            string filePath,
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            IReadableBitmap sourceBitmap = _mediaFactory.LoadReadableBitmap(filePath);

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
}
