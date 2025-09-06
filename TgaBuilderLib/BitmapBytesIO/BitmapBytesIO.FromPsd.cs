using Pfim;
using System;
using System.Collections.Generic;
using System.Drawing.PSD;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        public void FromPsd(
            string psdFilePath,
            ResizeMode mode = ResizeMode.SourceResize,
            CancellationToken? cancellationToken = null)
        {
            if (!File.Exists(psdFilePath))
                throw new FileNotFoundException("The specified file does not exist.", psdFilePath);

            var psd = new PsdFile();
            psd.Load(psdFilePath);

            var data = psd.ImageData;
            int bytesPerPixel = data.Length;

            LoadedHasAlpha = bytesPerPixel == 4;

            int originalWidth = psd.Columns;
            int originalHeight = psd.Rows;

            LoadedWidth = CalculatePaddedWidth(originalWidth, mode);
            LoadedHeight = CalculatePaddedHeight(originalHeight, mode);

            LoadedStride = LoadedWidth * bytesPerPixel;

            LoadedBytes = RentBlackPixelBuffer(
                width: LoadedWidth,
                height: LoadedHeight,
                hasAlpha: LoadedHasAlpha);

            for (int y = 0; y < originalHeight; y++)
            {
                for (int x = 0; x < originalWidth; x++)
                {
                    cancellationToken?.ThrowIfCancellationRequested();

                    int layerIndex = y * originalWidth + x;
                    int globalIndex = (y * LoadedWidth + x) * bytesPerPixel;

                    byte a = LoadedHasAlpha ? data[3][layerIndex] : (byte)255;
                    byte r = data[0][layerIndex];
                    byte g = data[1][layerIndex];
                    byte b = data[2][layerIndex];

                    if (LoadedHasAlpha)
                    {
                        LoadedBytes[globalIndex + 0] = b;
                        LoadedBytes[globalIndex + 1] = g;
                        LoadedBytes[globalIndex + 2] = r;
                        LoadedBytes[globalIndex + 3] = a;
                    }
                    else
                    {
                        LoadedBytes[globalIndex + 0] = r;
                        LoadedBytes[globalIndex + 1] = g;
                        LoadedBytes[globalIndex + 2] = b;
                    }
                }
            }
        }
    }
}
