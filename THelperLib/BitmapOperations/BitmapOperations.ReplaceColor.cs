using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace THelperLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public WriteableBitmap ReplaceColor(WriteableBitmap source, Color replacedColor, Color newColor)
        {
            if (source.Format != PixelFormats.Bgra32 && source.Format != PixelFormats.Rgb24)
                throw new ArgumentException("Unsupported pixel format. Only Bgra32 and Rgb24 are supported.");

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int sourceStride = width * 3;
            int targetStride = width * 3;

            WriteableBitmap result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Rgb24, null);

            source.Lock();
            result.Lock();

            byte r, g, b, a = 255;
            int sourceBbp = source.Format == PixelFormats.Rgb24 ? 3 : 4;

            unsafe
            {
                byte* srcPtr = (byte*)source.BackBuffer;
                byte* resPtr = (byte*)result.BackBuffer;

                for (int y = 0; y < height; y++)
                {
                    byte* srcRow = srcPtr + y * source.BackBufferStride;
                    byte* resRow = resPtr + y * result.BackBufferStride;

                    for (int x = 0; x < width; x++)
                    {
                        if (source.Format == PixelFormats.Bgra32)
                        {
                            b = srcRow[0];
                            g = srcRow[1];
                            r = srcRow[2];
                            a = srcRow[3];
                        }
                        else
                        {
                            r = srcRow[0];
                            g = srcRow[1];
                            b = srcRow[2];
                        }

                        if (a == 0 ||
                            (replacedColor.R == r && replacedColor.G == g && replacedColor.B == b))
                        {
                            // Replace with new color  
                            resRow[0] = newColor.R; // R  
                            resRow[1] = newColor.G; // G  
                            resRow[2] = newColor.B; // B  
                        }
                        else
                        {
                            resRow[0] = r; // R  
                            resRow[1] = g; // G  
                            resRow[2] = b; // B  
                        }

                        srcRow += sourceBbp;
                        resRow += 3;
                    }
                }
            }
            result.AddDirtyRect(new Int32Rect(0, 0, width, height));

            source.Unlock();
            result.Unlock();

            return result;
        }
    }
}
