using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace THelperLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void TileRally(WriteableBitmap bitmap, (int X, int Y) sourcePos, (int X, int Y) targetPos, int tileSize)
        {
            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) >> 3;

            int bitmapWidth = bitmap.PixelWidth;
            int bitmapHeight = bitmap.PixelHeight;

            int bitmapStride = bitmap.BackBufferStride;
            int tileStride = tileSize * bytesPerPixel;

            int sourceIndex = GetTexIndex(sourcePos, tileSize, bitmapWidth);
            int targetIndex = GetTexIndex(targetPos, tileSize, bitmapWidth);

            WriteableBitmap TileToInject = CropBitmap(bitmap, new Int32Rect(sourcePos.X, sourcePos.Y, tileSize, tileSize));

            List<(int X, int Y)> RequiredPositions = sourceIndex > targetIndex
                ? GetRequiredPositions(targetPos, sourceIndex - targetIndex, tileSize, bitmapWidth)
                : GetRequiredPositions(sourcePos, targetIndex - sourceIndex, tileSize, bitmapWidth);

            bitmap.Lock();

            unsafe
            {
                byte* originPtr = (byte*)bitmap.BackBuffer;
                byte* writePtr;
                byte* readPtr;

                if (sourceIndex > targetIndex)
                {
                    for (int i = RequiredPositions.Count - 1; i > 0; i--)
                    {
                        writePtr = originPtr + RequiredPositions[i].Y * bitmapStride + RequiredPositions[i].X * bytesPerPixel;
                        readPtr = originPtr + RequiredPositions[i - 1].Y * bitmapStride + RequiredPositions[i - 1].X * bytesPerPixel;
                        for (int r = 0; r < tileSize; r++)
                        {
                            for (int s = 0; s < tileStride; s++)
                            {
                                *writePtr++ = *readPtr++;
                            }
                            writePtr += bitmapStride - tileStride;
                            readPtr += bitmapStride - tileStride;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < RequiredPositions.Count - 1; i++)
                    {
                        readPtr = originPtr + RequiredPositions[i + 1].Y * bitmapStride + RequiredPositions[i + 1].X * bytesPerPixel;
                        writePtr = originPtr + RequiredPositions[i].Y * bitmapStride + RequiredPositions[i].X * bytesPerPixel;
                        for (int r = 0; r < tileSize; r++)
                        {
                            for (int s = 0; s < tileStride; s++)
                            {
                                *writePtr++ = *readPtr++;
                            }
                            writePtr += bitmapStride - tileStride;
                            readPtr += bitmapStride - tileStride;
                        }
                    }
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, RequiredPositions.First().Y, bitmapWidth,
                        RequiredPositions.Last().Y - RequiredPositions.First().Y + tileSize));
            bitmap.Unlock();

            FillRectBitmapNoConvert(TileToInject, bitmap, (targetPos.X, targetPos.Y));
        }

        private int GetTexIndex((int x, int y) anchor, int tileSize, int panelWidth)
        {
            return (anchor.x / tileSize) + (anchor.y / tileSize) * (panelWidth / tileSize);
        }

        private List<(int X, int Y)> GetRequiredPositions((int X, int Y) startAnchor, int requiredTiles, int tileSize, int panelWidth)
        {
            int x = startAnchor.X, y = startAnchor.Y;
            var res = new List<(int X, int Y)>();
            for (int i = 0; i <= requiredTiles; i++)
            {
                res.Add((x, y));
                x += tileSize;
                if (x >= panelWidth)  
                {
                    x = 0;
                    y += tileSize;
                }
            }
            return res;
        }
    }
}

