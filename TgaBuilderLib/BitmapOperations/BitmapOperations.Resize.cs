using System.Windows;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public WriteableBitmap Resize(WriteableBitmap sourceBitmap, int newWidth, int newHeight)
        {
            int bytesPerPixel = (sourceBitmap.Format.BitsPerPixel + 7) >> 3;

            // Create a new bitmap with the desired size
            var resizedBitmap = new WriteableBitmap(newWidth, newHeight, sourceBitmap.DpiX, sourceBitmap.DpiY, sourceBitmap.Format, null);

            int sourceStride = sourceBitmap.BackBufferStride;
            int resizedStride = resizedBitmap.BackBufferStride;

            sourceBitmap.Lock();
            resizedBitmap.Lock();

            unsafe
            {
                byte* sourcePtr = (byte*)sourceBitmap.BackBuffer;
                byte* resizedPtr = (byte*)resizedBitmap.BackBuffer;

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
            resizedBitmap.AddDirtyRect(new Int32Rect(0, 0, newWidth, newHeight));
            sourceBitmap.Unlock();
            resizedBitmap.Unlock();

            return resizedBitmap;
        }

        public WriteableBitmap ResizeWidthMonitored(WriteableBitmap sourceBitmap, int newWidth, byte[] undoData)
        {
            int bytesPerPixel = (sourceBitmap.Format.BitsPerPixel + 7) >> 3;
            int removedColumns = sourceBitmap.PixelWidth - newWidth;

            if (undoData.Length < removedColumns * sourceBitmap.PixelHeight * bytesPerPixel)
                throw new ArgumentException("Undo data size does not match the expected size for the given bitmap dimensions.");

            if (newWidth == sourceBitmap.PixelWidth)
                return sourceBitmap;

            if (newWidth > sourceBitmap.PixelWidth)
                return Resize(sourceBitmap, newWidth, sourceBitmap.PixelHeight);

            var resizedBitmap = new WriteableBitmap(newWidth, sourceBitmap.PixelHeight,
                sourceBitmap.DpiX, sourceBitmap.DpiY, sourceBitmap.Format, null);

            sourceBitmap.Lock();
            resizedBitmap.Lock();

            unsafe
            {
                byte* sourcePtr = (byte*)sourceBitmap.BackBuffer;
                byte* resizedPtr = (byte*)resizedBitmap.BackBuffer;

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

            resizedBitmap.AddDirtyRect(new Int32Rect(0, 0, newWidth, sourceBitmap.PixelHeight));
            sourceBitmap.Unlock();
            resizedBitmap.Unlock();

            return resizedBitmap;
        }

        public WriteableBitmap ResizeHeightMonitored(WriteableBitmap sourceBitmap, int newHeight, byte[] undoData)
        {
            int bytesPerPixel = (sourceBitmap.Format.BitsPerPixel + 7) >> 3;
            int removedRows = sourceBitmap.PixelHeight - newHeight;

            if (undoData.Length < removedRows * sourceBitmap.PixelWidth * bytesPerPixel)
                throw new ArgumentException("Undo data size does not match the expected size for the given bitmap dimensions.");

            if (newHeight == sourceBitmap.PixelHeight)
                return sourceBitmap;

            if (newHeight > sourceBitmap.PixelHeight)
                return Resize(sourceBitmap, sourceBitmap.PixelWidth, newHeight);

            var resizedBitmap = new WriteableBitmap(sourceBitmap.PixelWidth, newHeight,
                sourceBitmap.DpiX, sourceBitmap.DpiY, sourceBitmap.Format, null);

            sourceBitmap.Lock();
            resizedBitmap.Lock();

            unsafe
            {
                byte* sourcePtr = (byte*)sourceBitmap.BackBuffer;
                byte* resizedPtr = (byte*)resizedBitmap.BackBuffer;

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

            resizedBitmap.AddDirtyRect(new Int32Rect(0, 0, sourceBitmap.PixelWidth, newHeight));
            sourceBitmap.Unlock();
            resizedBitmap.Unlock();

            return resizedBitmap;
        }
    }
}
