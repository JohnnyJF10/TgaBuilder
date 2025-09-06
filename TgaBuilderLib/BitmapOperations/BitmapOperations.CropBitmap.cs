using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public IWriteableBitmap CropBitmap(IWriteableBitmap source, PixelRect rectangle)
        {
            int bytesPerPixel = source.HasAlpha ? 4 : 3;

            int rectX = Math.Max(0, rectangle.X);
            int rectY = Math.Max(0, rectangle.Y);
            int recWidth = Math.Min(rectangle.Width, source.PixelWidth - rectX);
            int recHeight = Math.Min(rectangle.Height, source.PixelHeight - rectY);

            IWriteableBitmap target = _mediaFactory.CreateEmptyBitmap(
                width:          recWidth,
                height:         recHeight,
                hasAlpha:       source.HasAlpha);

            int sourceStride = source.BackBufferStride;
            int targetStride = target.BackBufferStride;
            int strideDelta = sourceStride - targetStride;

            source.Lock();
            target.Lock();

            unsafe
            {
                byte* sourcePtr = (byte*)source.BackBuffer;
                byte* targetPtr = (byte*)target.BackBuffer;

                sourcePtr += rectY * sourceStride + rectX * bytesPerPixel;

                for (int r = 0; r < recHeight; r++)
                {
                    for (int s = 0; s < targetStride; s++)
                    {
                        *targetPtr++ = *sourcePtr++;
                    }
                    sourcePtr += strideDelta;
                }
            }
            target.AddDirtyRect(new PixelRect(0, 0, recWidth, recHeight));
            source.Unlock();
            target.Unlock();

            return target;
        }
        
        public IReadableBitmap CropIReadableBitmap(IReadableBitmap source, PixelRect rectangle, byte[]? pixelbuffer = null)
        {
            int bytesPerPixel = source.HasAlpha ? 4 : 3;

            int rectX = Math.Max(0, rectangle.X);
            int rectY = Math.Max(0, rectangle.Y);
            int recWidth = Math.Min(rectangle.Width, source.PixelWidth - rectX);
            int recHeight = Math.Min(rectangle.Height, source.PixelHeight - rectY);
        
            if (recWidth <= 0 || recHeight <= 0)
                throw new ArgumentException("The specified rectangle is out of the bounds of the source bitmap.");
        
            int stride = recWidth * bytesPerPixel;

            byte[] pixelData = pixelbuffer is not null && pixelbuffer.Length >= recHeight * stride
                ? pixelbuffer
                : new byte[recHeight * stride];

            source.CopyPixels(
                new PixelRect(rectX, rectY, recWidth, recHeight),
                pixelData,
                stride,
                0);
        
            return _mediaFactory.CreateBitmapFromRaw(
                recWidth,
                recHeight,
                source.HasAlpha,
                pixelData,
                stride);
        }

        public IWriteableBitmap CropBitmap(IWriteableBitmap source, PixelRect rectangle, Color replacedColor, Color newColor)
        {
            int rectX = Math.Max(0, rectangle.X);
            int rectY = Math.Max(0, rectangle.Y);
            int recWidth = Math.Min(rectangle.Width, source.PixelWidth - rectX);
            int recHeight = Math.Min(rectangle.Height, source.PixelHeight - rectY);

            IWriteableBitmap target = _mediaFactory.CreateEmptyBitmap(
                width: recWidth,
                height: recHeight,
                hasAlpha: source.HasAlpha);

            source.Lock();
            target.Lock();

            byte r, g, b, a = 255;
            int bpp = source.HasAlpha ? 4 : 3;

            unsafe
            {
                byte* srcPtr = (byte*)source.BackBuffer;
                byte* resPtr = (byte*)target.BackBuffer;

                for (int y = 0; y < recHeight; y++)
                {
                    byte* srcRow = srcPtr + (y + rectY) * source.BackBufferStride + bpp * rectX;
                    byte* resRow = resPtr + y * target.BackBufferStride;

                    for (int x = 0; x < recWidth; x++)
                    {
                        if (source.HasAlpha)
                        {
                            b = srcRow[0];
                            g = srcRow[1];
                            r = srcRow[2];
                            a = srcRow[3];

                            if (a == 0 || (replacedColor.R, replacedColor.G, replacedColor.B, replacedColor.A) == (r, g, b, a))
                            {
                                // Replace with new color  
                                resRow[0] = newColor.B;        // B  
                                resRow[1] = newColor.G;        // G  
                                resRow[2] = newColor.R;        // R  
                                resRow[3] = newColor.A ?? 255; // A  
                            }
                            else
                            {
                                resRow[0] = b; // B  
                                resRow[1] = g; // G  
                                resRow[2] = r; // R  
                                resRow[3] = a; // A  
                            }
                        }
                        else
                        {
                            r = srcRow[0];
                            g = srcRow[1];
                            b = srcRow[2];

                            if (a == 0 || (replacedColor.R, replacedColor.G, replacedColor.B) == (r, g, b))
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
                        }

                        srcRow += bpp;
                        resRow += bpp;
                    }
                }
            }

            target.AddDirtyRect(new PixelRect(0, 0, recWidth, recHeight));
            source.Unlock();
            target.Unlock();

            return target;
        }
    }
}
