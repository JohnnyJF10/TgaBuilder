using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public IWriteableBitmap Resize(IWriteableBitmap sourceBitmap, int newWidth, int newHeight)
        {
            int bytesPerPixel = sourceBitmap.HasAlpha ? 4 : 3;

            // Create a new bitmap with the desired size

            IWriteableBitmap resizedBitmap = _mediaFactory.CreateEmptyBitmap(
                width:          newWidth, 
                height:         newHeight, 
                hasAlpha:       bytesPerPixel == 4);

            int sourceStride = sourceBitmap.BackBufferStride;
            int resizedStride = resizedBitmap.BackBufferStride;

            using var sourceLocker = sourceBitmap.GetLocker();
            using var resizedLocker = resizedBitmap.GetLocker(requiresRefresh: true);

            unsafe
            {
                byte* sourcePtr = (byte*)sourceLocker.BackBuffer;
                byte* resizedPtr = (byte*)resizedLocker.BackBuffer;

                int widthLimit = Math.Min(sourceBitmap.PixelWidth, newWidth);
                int heightLimit = Math.Min(sourceBitmap.PixelHeight, newHeight);

                // Copy the existing pixels to the new bitmap
                for (int y = 0; y < heightLimit; y++)
                {
                    for (int x = 0; x < widthLimit; x++)
                    {
                        int sourceIndex = y * sourceStride + x * bytesPerPixel;
                        int resizedIndex = y * resizedStride + x * bytesPerPixel;
                        for (int b = 0; b < bytesPerPixel; b++)
                        {
                            resizedPtr[resizedIndex + b] = sourcePtr[sourceIndex + b];
                        }
                    }
                }

                // If the new bitmap is larger, fill the remaining area with zeroes
                if (newWidth > sourceBitmap.PixelWidth || newHeight > sourceBitmap.PixelHeight)
                {
                    for (int y = 0; y < newHeight; y++)
                    {
                        for (int x = widthLimit; x < newWidth; x++)
                        {
                            int resizedIndex = y * resizedStride + x * bytesPerPixel;
                            for (int b = 0; b < bytesPerPixel; b++)
                            {
                                resizedPtr[resizedIndex + b] = 0;
                            }
                        }
                    }

                    for (int y = heightLimit; y < newHeight; y++)
                    {
                        for (int x = 0; x < newWidth; x++)
                        {
                            int resizedIndex = y * resizedStride + x * bytesPerPixel;
                            for (int b = 0; b < bytesPerPixel; b++)
                            {
                                resizedPtr[resizedIndex + b] = 0;
                            }
                        }
                    }
                }
            }


            return resizedBitmap;
        }

        public IWriteableBitmap ResizeWidthMonitored(IWriteableBitmap sourceBitmap, int newWidth, byte[] undoData)
        {
            int bytesPerPixel = sourceBitmap.HasAlpha ? 4 : 3;
            int removedColumns = sourceBitmap.PixelWidth - newWidth;

            if (undoData.Length < removedColumns * sourceBitmap.PixelHeight * bytesPerPixel)
                throw new ArgumentException("Undo data size does not match the expected size for the given bitmap dimensions.");

            if (newWidth == sourceBitmap.PixelWidth)
                return sourceBitmap;

            if (newWidth > sourceBitmap.PixelWidth)
                return Resize(sourceBitmap, newWidth, sourceBitmap.PixelHeight);

            IWriteableBitmap resizedBitmap = _mediaFactory.CreateEmptyBitmap(
                width: newWidth,
                height: sourceBitmap.PixelHeight,
                hasAlpha: sourceBitmap.HasAlpha);

            using var sourceLocker = sourceBitmap.GetLocker();
            using var resizedLocker = resizedBitmap.GetLocker(requiresRefresh: true);

            unsafe
            {
                byte* sourcePtr = (byte*)sourceLocker.BackBuffer;
                byte* resizedPtr = (byte*)resizedLocker.BackBuffer;

                int sourceStride = sourceBitmap.BackBufferStride;
                int resizedStride = resizedBitmap.BackBufferStride;

                int undoIndex = 0;

                for (int y = 0; y < sourceBitmap.PixelHeight; y++)
                {
                    // Copy pixels that remain
                    for (int x = 0; x < newWidth; x++)
                    {
                        int sourceIndex = y * sourceStride + x * bytesPerPixel;
                        int resizedIndex = y * resizedStride + x * bytesPerPixel;

                        for (int b = 0; b < bytesPerPixel; b++)
                        {
                            resizedPtr[resizedIndex + b] = sourcePtr[sourceIndex + b];
                        }
                    }

                    // Store pixels that are being removed
                    for (int x = newWidth; x < sourceBitmap.PixelWidth; x++)
                    {
                        int sourceIndex = y * sourceStride + x * bytesPerPixel;

                        for (int b = 0; b < bytesPerPixel; b++)
                        {
                            undoData[undoIndex++] = sourcePtr[sourceIndex + b];
                        }
                    }
                }
            }

            return resizedBitmap;
        }

        public IWriteableBitmap ResizeHeightMonitored(IWriteableBitmap sourceBitmap, int newHeight, byte[] undoData)
        {
            int bytesPerPixel = sourceBitmap.HasAlpha ? 4 : 3;
            int removedRows = sourceBitmap.PixelHeight - newHeight;

            if (undoData.Length < removedRows * sourceBitmap.PixelWidth * bytesPerPixel)
                throw new ArgumentException("Undo data size does not match the expected size for the given bitmap dimensions.");

            if (newHeight == sourceBitmap.PixelHeight)
                return sourceBitmap;

            if (newHeight > sourceBitmap.PixelHeight)
                return Resize(sourceBitmap, sourceBitmap.PixelWidth, newHeight);

            IWriteableBitmap resizedBitmap = _mediaFactory.CreateEmptyBitmap(
                width: sourceBitmap.PixelWidth,
                height: newHeight,
                hasAlpha: sourceBitmap.HasAlpha);

            using var sourceLocker = sourceBitmap.GetLocker();
            using var resizedLocker = resizedBitmap.GetLocker(requiresRefresh: true);

            unsafe
            {
                byte* sourcePtr = (byte*)sourceLocker.BackBuffer;
                byte* resizedPtr = (byte*)resizedLocker.BackBuffer;

                int sourceStride = sourceBitmap.BackBufferStride;
                int resizedStride = resizedBitmap.BackBufferStride;

                int undoIndex = 0;

                // Copy rows that remain
                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                    {
                        int sourceIndex = y * sourceStride + x * bytesPerPixel;
                        int resizedIndex = y * resizedStride + x * bytesPerPixel;

                        for (int b = 0; b < bytesPerPixel; b++)
                        {
                            resizedPtr[resizedIndex + b] = sourcePtr[sourceIndex + b];
                        }
                    }
                }

                // Store rows that are being removed
                for (int y = newHeight; y < sourceBitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                    {
                        int sourceIndex = y * sourceStride + x * bytesPerPixel;

                        for (int b = 0; b < bytesPerPixel; b++)
                        {
                            undoData[undoIndex++] = sourcePtr[sourceIndex + b];
                        }
                    }
                }
            }

            return resizedBitmap;
        }
    }
}
