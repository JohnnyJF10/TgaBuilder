using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public WriteableBitmap GetTargetFromSource(WriteableBitmap source) 
        {
            if (source.Format == PixelFormats.Rgb24)
                return CropBitmap(
                    source:     source, 
                    rectangle:  new Int32Rect(
                        x:      0,
                        y:      0,
                        width:  EstimateTargetWidth(source.PixelWidth),
                        height: source.PixelHeight));

            if (source.Format != PixelFormats.Bgra32)
                throw new ArgumentException("Source must be in Bgra32 or Rgb24 format");

            int sourceWidth = source.PixelWidth;
            int height = source.PixelHeight;
            int targetWidth = EstimateTargetWidth(sourceWidth);

            WriteableBitmap target = new WriteableBitmap(
                pixelWidth:     targetWidth,
                pixelHeight:    height,
                dpiX:           source.DpiX,
                dpiY:           source.DpiY,
                pixelFormat:    PixelFormats.Rgb24,
                palette:        null);

            source.Lock();
            target.Lock();

            unsafe
            {
                byte* srcPtr = (byte*)source.BackBuffer;
                byte* dstPtr = (byte*)target.BackBuffer;

                int sourceStride = source.BackBufferStride;
                int targetStride = target.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    byte* srcRow = srcPtr + y * sourceStride;
                    byte* dstRow = dstPtr + y * targetStride;

                    for (int x = 0; x < targetWidth; x++)
                    {
                        if (x < sourceWidth)
                        {
                            byte b = srcRow[x * 4 + 0];
                            byte g = srcRow[x * 4 + 1];
                            byte r = srcRow[x * 4 + 2];
                            byte a = srcRow[x * 4 + 3];

                            if (a != 0)
                            {
                                dstRow[x * 3 + 0] = r;
                                dstRow[x * 3 + 1] = g;
                                dstRow[x * 3 + 2] = b;
                            }
                            else
                            {
                                dstRow[x * 3 + 0] = 255; // R
                                dstRow[x * 3 + 1] = 0;   // G
                                dstRow[x * 3 + 2] = 255; // B
                            }
                        }
                        else
                        {
                            // Magenta 
                            dstRow[x * 3 + 0] = 255; // R
                            dstRow[x * 3 + 1] = 0;   // G
                            dstRow[x * 3 + 2] = 255; // B
                        }
                    }
                }

                target.AddDirtyRect(new Int32Rect(0, 0, targetWidth, height));
            }

            source.Unlock();
            target.Unlock();

            return target;
        }

        private int EstimateTargetWidth(int sourceWidth) => sourceWidth switch
            {
                <= 256  => 256,
                <= 512  => 512,
                <= 1024 => 1024,
                <= 2048 => 2048,
                _       => 4096,
            };
    }
}
