using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.FileHandling;

namespace TgaBuilderLib.BitmapIO
{
    public partial class BitmapIO : IBitmapIO
    {
        private const int MAX_SIZE = 32768;
        private const int TR_PAGE_SIZE = 256;
        private const int MAX_TARGET_WIDTH = 16 * TR_PAGE_SIZE;

        public ResultStatus ResultInfo { get; private set; } = ResultStatus.Success;


        private void ValidateImageInput(string filePath, PixelFormat format)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            if (format != PixelFormats.Bgra32 && format != PixelFormats.Rgb24)
                throw new NotSupportedException("Only PixelFormats.Bgra32 and PixelFormats.Rgb24 are supported for conversion.");
        }

        private int CalculatePaddedWidth(int width, ResizeMode mode)
        {
            int paddedWidth = RoundUpToNextMultiple(width, TR_PAGE_SIZE);

            paddedWidth = Math.Clamp(
                value: paddedWidth,
                min: TR_PAGE_SIZE,
                max: mode == ResizeMode.TargetResize ? MAX_TARGET_WIDTH : MAX_SIZE);

            if (paddedWidth != width)
                ResultInfo = ResultStatus.RezisingRequired;

            return paddedWidth;
        }

        private int CalculatePaddedHeight(int height, ResizeMode mode)
        {
            int paddedHeight = RoundUpToNextMultiple(height, TR_PAGE_SIZE);

            paddedHeight = Math.Clamp(
                value: paddedHeight,
                min: TR_PAGE_SIZE,
                max: MAX_SIZE);

            if (paddedHeight != height)
                ResultInfo = ResultStatus.RezisingRequired;

            return paddedHeight;
        }

        private byte[] CreateBlackPixelBuffer(int width, int height, int bytesPerPixel, byte alpha = 255)
        {
            int stride = width * bytesPerPixel;
            byte[] buffer = new byte[height * stride];

            for (int i = 0; i < buffer.Length; i += bytesPerPixel)
            {
                buffer[i + 0] = 0; // B
                buffer[i + 1] = 0; // G
                buffer[i + 2] = 0; // R
                if (bytesPerPixel == 4)
                    buffer[i + 3] = alpha; // A
            }

            return buffer;
        }

        private int RoundUpToNextMultiple(int number, int multiple)
            => multiple == 0 ? number : (number + multiple - 1) / multiple * multiple;

    }
}
