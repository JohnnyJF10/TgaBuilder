using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Enums;
using TgaBuilderLib.FileHandling;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO : IBitmapBytesIO
    {
        public BitmapBytesIO(
            Func<int, int, bool, WriteableBitmap> bitmapFactory)
        {
            _bitmapFactory = bitmapFactory;
        }

        private const int MAX_SIZE = 32768;
        private const int TR_PAGE_SIZE = 256;
        private const int MAX_TARGET_WIDTH = 16 * TR_PAGE_SIZE;

        private readonly ArrayPool<byte> _bytesPool = ArrayPool<byte>.Shared;
        private readonly Func<int, int, bool, WriteableBitmap> _bitmapFactory;

        public ResultStatus ResultInfo { get; private set; } = ResultStatus.Success;

        public byte[]? LoadedBytes { get; private set; }

        public int LoadedWidth { get; private set; }
        public int LoadedHeight { get; private set; }

        public int LoadedStride { get; private set; }

        public bool LoadedHasAlpha { get; private set; }

        public int ActualDataLength { get; private set; }

        public WriteableBitmap GetLoadedBitmap()
        {
            if (LoadedBytes == null)
                throw new InvalidOperationException("No image data loaded.");

            var wb = GetNewBitmap(
                width:      LoadedWidth,
                height:     LoadedHeight,
                hasAlpha:   LoadedHasAlpha);

            wb.WritePixels(
                sourceRect: new System.Windows.Int32Rect(0, 0, LoadedWidth, LoadedHeight),
                pixels: LoadedBytes,
                stride: LoadedStride,
                offset: 0);

            return wb;
        }

        public void ClearLoadedData()
        {
            if (LoadedBytes != null)
            {
                _bytesPool.Return(LoadedBytes);
                LoadedBytes = null;
            }
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

        private byte[] RentBlackPixelBuffer(int width, int height, bool hasAlpha, byte alpha = 255)
        {
            int bytesPerPixel = hasAlpha ? 4 : 3;
            int stride = width * bytesPerPixel;
            int desiredSize = height * stride;
            byte[] buffer = _bytesPool.Rent(desiredSize);

            for (int i = 0; i < desiredSize; i += bytesPerPixel)
            {
                buffer[i + 0] = 0; // B
                buffer[i + 1] = 0; // G
                buffer[i + 2] = 0; // R
                if (hasAlpha)
                    buffer[i + 3] = alpha; // A
            }

            return buffer;
        }

        private int RoundUpToNextMultiple(int number, int multiple)
            => multiple == 0 ? number : (number + multiple - 1) / multiple * multiple;

        private WriteableBitmap GetNewBitmap(int width, int height, bool hasAlpha)
            => _bitmapFactory.Invoke(width, height, hasAlpha);
    }
}
