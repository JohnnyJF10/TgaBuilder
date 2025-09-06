using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapBytesIO
{
    public partial class BitmapBytesIO
    {
        public IWriteableBitmap FromOtherBitmap(IWriteableBitmap source)
        {
            int sourceWidth = source.PixelWidth;
            int height = source.PixelHeight;
            int targetWidth = EstimateTargetWidth(sourceWidth);

            IWriteableBitmap target = _mediaFactory.CreateEmptyBitmap(
                width: targetWidth,
                height: height,
                hasAlpha: source.HasAlpha);

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

                    for (int b = 0; b < targetStride; b++)
                    {
                        if (b < sourceStride)
                        {
                            *dstRow++ = *srcRow++;
                        }

                    }
                }

            }

            target.AddDirtyRect(new PixelRect(0, 0, targetWidth, height));
            source.Unlock();
            target.Unlock();

            return target;
        }

        private int EstimateTargetWidth(int sourceWidth) => sourceWidth switch
        {
            <= 256 => 256,
            <= 512 => 512,
            <= 1024 => 1024,
            <= 2048 => 2048,
            _ => 4096,
        };

    }
}
