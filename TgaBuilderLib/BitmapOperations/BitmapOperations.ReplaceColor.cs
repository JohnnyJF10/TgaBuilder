using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public IWriteableBitmap ReplaceColor(IWriteableBitmap source, Color replacedColor, Color newColor)
            => source.HasAlpha 
            ? ReplaceColor32(source, replacedColor, newColor) 
            : ReplaceColor24(source, replacedColor, newColor);
        

        private IWriteableBitmap ReplaceColor24(IWriteableBitmap source, Color replacedColor, Color newColor)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int sourceStride = width * 3;
            int targetStride = width * 3;

            IWriteableBitmap result = _mediaFactory.CreateEmptyBitmap(
                width:          width,
                height:         height,
                hasAlpha:       false);

            source.Lock();
            result.Lock();

            byte r, g, b, a = 255;

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
                        r = srcRow[0];
                        g = srcRow[1];
                        b = srcRow[2];

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

                        srcRow += 3;
                        resRow += 3;
                    }
                }
            }
            result.AddDirtyRect(new PixelRect(0, 0, width, height));

            source.Unlock();
            result.Unlock();

            return result;
        }

        private IWriteableBitmap ReplaceColor32(IWriteableBitmap source, Color replacedColor, Color newColor)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int sourceStride = width * 3;
            int targetStride = width * 3;

            IWriteableBitmap result = _mediaFactory.CreateEmptyBitmap(
                width: width,
                height: height,
                hasAlpha: true);

            source.Lock();
            result.Lock();

            byte r, g, b, a = 255;
            
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
                        b = srcRow[0];
                        g = srcRow[1];
                        r = srcRow[2];
                        a = srcRow[3];

                        if (a == 0 ||
                            (replacedColor.R == r && replacedColor.G == g && replacedColor.B == b))
                        {
                            // Replace with new color  
                            resRow[0] = newColor.B;        // B 
                            resRow[1] = newColor.G;        // G  
                            resRow[2] = newColor.R;        // R  
                            resRow[3] = newColor.A ?? 255; // A
                        }
                        else
                        {
                            resRow[0] = b; // R  
                            resRow[1] = g; // G  
                            resRow[2] = r; // B  
                            resRow[3] = a; // A
                        }

                        srcRow += 4;
                        resRow += 4;
                    }
                }
            }
            result.AddDirtyRect(new PixelRect(0, 0, width, height));

            source.Unlock();
            result.Unlock();

            return result;
        }

    }
}
