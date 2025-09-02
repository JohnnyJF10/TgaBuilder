using System.Windows.Media.Imaging;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public WriteableBitmap ResizeSorted(WriteableBitmap oldBitmap, int newWidth, int tileSize, int newHeight = -1)
        {
            const int PAGE_SIZE = 256;
            const int MAX_PIXEL_HEIGHT = 32768;

            int bytesPerPixel = (oldBitmap.Format.BitsPerPixel + 7) >> 3;

            int oldWidth = oldBitmap.PixelWidth;
            int oldHeight = oldBitmap.PixelHeight;

            float widthRatio = newWidth / (float)oldWidth;

            if (newHeight == -1)
            {
                newHeight = (int)(oldHeight / widthRatio);
            }

            if (newHeight > MAX_PIXEL_HEIGHT)
            {
                throw new ArgumentOutOfRangeException(nameof(newHeight), $"Height cannot exceed {MAX_PIXEL_HEIGHT} pixels.");
            }

            if (newHeight % PAGE_SIZE != 0)
            {
                newHeight = (newHeight / PAGE_SIZE + 1) * PAGE_SIZE;
            }

            // Create a new bitmap with the desired size
            WriteableBitmap newBitmap = GetNewWriteableBitmap(
                width:          newWidth,
                height:         newHeight,
                hasAlpha:       bytesPerPixel == 4);

            int oldStride = oldBitmap.BackBufferStride;
            int newStride = newBitmap.BackBufferStride;
            int tileStride = tileSize * bytesPerPixel;

            var Positions = GetAllPositions(tileSize, oldWidth, oldHeight, newWidth, newHeight);

            oldBitmap.Lock();
            newBitmap.Lock();

            unsafe
            {
                byte* oldOriginPtr = (byte*)oldBitmap.BackBuffer;
                byte* newOriginPtr = (byte*)newBitmap.BackBuffer;
                byte* oldPtr;
                byte* newPtr;

                foreach (var (oldX, oldY, newX, newY) in Positions)
                {
                    oldPtr = oldOriginPtr + oldY * oldStride + oldX * bytesPerPixel;
                    newPtr = newOriginPtr + newY * newStride + newX * bytesPerPixel;
                    for (int r = 0; r < tileSize; r++)
                    {
                        for (int s = 0; s < tileStride; s++)
                        {
                            *newPtr++ = *oldPtr++;
                        }
                        newPtr += newStride - tileStride;
                        oldPtr += oldStride - tileStride;
                    }
                }

                if (newWidth > oldBitmap.PixelWidth || newHeight > oldBitmap.PixelHeight)
                {
 
                }

                oldBitmap.Unlock();
                newBitmap.Unlock();
            }

            return newBitmap;
        }

        private List<(int oldX, int oldY, int newX, int newY)> GetAllPositions(
            int tileSize, int oldPanelWidth, int oldPanelHeight, int newPanelWidth, int newPanelHeight)
        {
            int oldx = 0, oldy = 0, newx = 0, newy = 0;
            int tileCount = (oldPanelWidth / tileSize) * (oldPanelHeight / tileSize) - 1;
            var res = new List<(int oldX, int oldY, int newX, int newY)>();
            for (int i = 0; i <= tileCount; i++)
            {
                res.Add((oldx, oldy, newx, newy));
                oldx += tileSize;
                newx += tileSize;
                if (oldx >= oldPanelWidth)  
                {
                    oldx = 0;
                    oldy += tileSize;
                }
                if (newx >= newPanelWidth)
                {
                    newx = 0;
                    newy += tileSize;
                    if (newy >= newPanelHeight) break;
                }
            }
            return res;
        }
    }
}
