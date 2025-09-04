using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public IWriteableBitmap? SwapBitmap { get; set; }

        public int PlacedSize { get; set; }

        public void FillRectBitmap(
            IWriteableBitmap source, 
            IWriteableBitmap target,
            (int X, int Y) pos, 
            byte[] undoPixels, 
            byte[] redoPixels,
            double opacity = 1.0,
            PlacingMode placingMode = PlacingMode.Default)
        {
            if (target.HasAlpha)
                FillRectBitmap32(source, target, pos, undoPixels, redoPixels, opacity, placingMode);
            else
                FillRectBitmap32(source, target, pos, undoPixels, redoPixels, opacity, placingMode);
        }

        private void FillRectBitmap24(
            IWriteableBitmap source,
            IWriteableBitmap target,
            (int X, int Y) pos,
            byte[] undoPixels,
            byte[] redoPixels,
            double opacity = 1.0,
            PlacingMode placingMode = PlacingMode.Default)
        {
            int sWidth = source.PixelWidth;
            int sHeight = source.PixelHeight;

            int posX = Math.Max(0, pos.X);
            int posY = Math.Max(0, pos.Y);

            int tWidth = target.PixelWidth;
            int tHeight = target.PixelHeight;

            sWidth = Math.Min(sWidth, tWidth - posX);
            sHeight = Math.Min(sHeight, tHeight - posY);

            if (undoPixels.Length < sWidth * sHeight * 3)
                throw new ArgumentException("Monitored UndoPixels length must match source bitmap size.");

            if (redoPixels.Length < sWidth * sHeight * 3)
                throw new ArgumentException("Monitored RedoPixels length must match target bitmap size.");

            if (sWidth <= 0 || sHeight <= 0)
                throw new ArgumentException("Source bitmap dimensions must be greater than zero.");

            bool OverlayTransparent =
                (placingMode & PlacingMode.OverlayTransparent) == PlacingMode.OverlayTransparent;

            bool SwapAndPlace =
                (placingMode & PlacingMode.PlaceAndSwap) == PlacingMode.PlaceAndSwap;

            if (SwapAndPlace && SwapBitmap is null)
                throw new ArgumentException("SwapBitmap must be set when OverlayTransparent is used.");

            if (sWidth <= 0 || sHeight <= 0) return;

            source.Lock();
            target.Lock();
            SwapBitmap?.Lock();

            unsafe
            {
                byte* srcBase = (byte*)source.BackBuffer;
                byte* tgtBase = (byte*)target.BackBuffer;
                byte* swapBase = SwapBitmap != null ? (byte*)SwapBitmap.BackBuffer : null;

                int srcStride = source.BackBufferStride;
                int tgtStride = target.BackBufferStride;
                int srcBpp = source.HasAlpha ? 4 : 3;

                fixed (byte* pUndoPixels = undoPixels, pRedoPixels = redoPixels)
                {
                    byte* undoLine = pUndoPixels;
                    byte* redoLine = pRedoPixels;

                    for (int y = 0; y < sHeight; y++)
                    {
                        byte* srcLine = srcBase + y * srcStride;
                        byte* tgtLine = tgtBase + (posY + y) * tgtStride + posX * 3;
                        byte* swapLine = SwapBitmap != null ? swapBase + y * SwapBitmap.BackBufferStride : null;

                        for (int x = 0; x < sWidth; x++)
                        {
                            byte r = 0, g = 0, b = 0, a = 255;

                            if (!source.HasAlpha)
                            {
                                r = srcLine[0];
                                g = srcLine[1];
                                b = srcLine[2];
                            }
                            else
                            {
                                b = srcLine[0];
                                g = srcLine[1];
                                r = srcLine[2];
                                a = srcLine[3];
                            }

                            a = (byte)(a * opacity);

                            undoLine[0] = tgtLine[0];
                            undoLine[1] = tgtLine[1];
                            undoLine[2] = tgtLine[2];

                            if (swapLine != null)
                            {
                                swapLine[0] = tgtLine[0];
                                swapLine[1] = tgtLine[1];
                                swapLine[2] = tgtLine[2];
                            }

                            if (a == 255 && (!OverlayTransparent || (r, g, b) != (255, 0, 255)))
                            {
                                tgtLine[0] = r;
                                tgtLine[1] = g;
                                tgtLine[2] = b;
                            }
                            else if ((a == 0 && !OverlayTransparent) || (srcBpp == 3 && ((r, g, b) == (255, 0, 255) && !OverlayTransparent)))
                            {
                                tgtLine[0] = 255;
                                tgtLine[1] = 0;
                                tgtLine[2] = 255;
                            }
                            else if (a < 255 && (srcBpp > 3 || (srcBpp == 3 && ((r, g, b) != (255, 0, 255)))))
                            {
                                tgtLine[0] = (byte)((tgtLine[0] * (255 - a) + r * a) / 255);
                                tgtLine[1] = (byte)((tgtLine[1] * (255 - a) + g * a) / 255);
                                tgtLine[2] = (byte)((tgtLine[2] * (255 - a) + b * a) / 255);
                            }

                            redoLine[0] = tgtLine[0];
                            redoLine[1] = tgtLine[1];
                            redoLine[2] = tgtLine[2];

                            srcLine += srcBpp;
                            tgtLine += 3;
                            redoLine += 3;
                            undoLine += 3;
                            if (swapLine != null)
                                swapLine += 3;
                        }
                    }
                }
            }
            target.AddDirtyRect(new PixelRect(posX, posY, sWidth, sHeight));
            SwapBitmap?.AddDirtyRect(new PixelRect(0, 0, sWidth, sHeight));

            SwapBitmap?.Unlock();
            target.Unlock();
            source.Unlock();
        }

        private void FillRectBitmap32(
            IWriteableBitmap source,
            IWriteableBitmap target,
            (int X, int Y) pos,
            byte[] undoPixels,
            byte[] redoPixels,
            double opacity = 1.0,
            PlacingMode placingMode = PlacingMode.Default)
        {
            int sWidth = source.PixelWidth;
            int sHeight = source.PixelHeight;

            int posX = Math.Max(0, pos.X);
            int posY = Math.Max(0, pos.Y);

            int tWidth = target.PixelWidth;
            int tHeight = target.PixelHeight;

            sWidth = Math.Min(sWidth, tWidth - posX);
            sHeight = Math.Min(sHeight, tHeight - posY);

            if (undoPixels.Length < sWidth * sHeight * 4)
                throw new ArgumentException("Monitored UndoPixels length must match source bitmap size.");

            if (redoPixels.Length < sWidth * sHeight * 4)
                throw new ArgumentException("Monitored RedoPixels length must match target bitmap size.");

            if (sWidth <= 0 || sHeight <= 0)
                throw new ArgumentException("Source bitmap dimensions must be greater than zero.");

            bool OverlayTransparent =
                (placingMode & PlacingMode.OverlayTransparent) == PlacingMode.OverlayTransparent;

            bool SwapAndPlace =
                (placingMode & PlacingMode.PlaceAndSwap) == PlacingMode.PlaceAndSwap;

            if (SwapAndPlace && SwapBitmap is null)
                throw new ArgumentException("SwapBitmap must be set when OverlayTransparent is used.");

            if (sWidth <= 0 || sHeight <= 0) return;

            source.Lock();
            target.Lock();
            SwapBitmap?.Lock();

            unsafe
            {
                byte* srcBase = (byte*)source.BackBuffer;
                byte* tgtBase = (byte*)target.BackBuffer;
                byte* swapBase = SwapBitmap != null ? (byte*)SwapBitmap.BackBuffer : null;

                int srcStride = source.BackBufferStride;
                int tgtStride = target.BackBufferStride;
                int srcBpp = source.HasAlpha ? 4 : 3;

                fixed (byte* pUndoPixels = undoPixels, pRedoPixels = redoPixels)
                {
                    byte* undoLine = pUndoPixels;
                    byte* redoLine = pRedoPixels;

                    for (int y = 0; y < sHeight; y++)
                    {
                        byte* srcLine = srcBase + y * srcStride;
                        byte* tgtLine = tgtBase + (posY + y) * tgtStride + posX * 4;
                        byte* swapLine = SwapBitmap != null ? swapBase + y * SwapBitmap.BackBufferStride : null;

                        for (int x = 0; x < sWidth; x++)
                        {
                            byte r = 0, g = 0, b = 0, a = 255;

                            if (!source.HasAlpha)
                            {
                                r = srcLine[0];
                                g = srcLine[1];
                                b = srcLine[2];
                            }
                            else
                            {
                                b = srcLine[0];
                                g = srcLine[1];
                                r = srcLine[2];
                                a = srcLine[3];
                            }

                            a = (byte)(a * opacity);

                            undoLine[0] = tgtLine[0];
                            undoLine[1] = tgtLine[1];
                            undoLine[2] = tgtLine[2];
                            undoLine[3] = tgtLine[3];

                            if (swapLine != null)
                            {
                                swapLine[0] = tgtLine[0];
                                swapLine[1] = tgtLine[1];
                                swapLine[2] = tgtLine[2];
                                swapLine[3] = tgtLine[3];
                            }

                            if (!OverlayTransparent && (srcBpp > 3 || (srcBpp == 3 && ((r, g, b) != (255, 0, 255)))))
                            {
                                tgtLine[0] = b;
                                tgtLine[1] = g;
                                tgtLine[2] = r;
                                tgtLine[3] = a;
                            }
                            else if ((srcBpp > 3 && a == 0 && !OverlayTransparent) || (srcBpp == 3 && ((r, g, b) == (255, 0, 255) && !OverlayTransparent)) )
                            {
                                tgtLine[0] = 0;
                                tgtLine[1] = 0;
                                tgtLine[2] = 0;
                                tgtLine[3] = 0;
                            }
                            else if (srcBpp > 3 || (srcBpp == 3 && ((r, g, b) != (255, 0, 255))))
                            {
                                tgtLine[0] = (byte)((tgtLine[0] * (255 - a) + b * a) / 255);
                                tgtLine[1] = (byte)((tgtLine[1] * (255 - a) + g * a) / 255);
                                tgtLine[2] = (byte)((tgtLine[2] * (255 - a) + r * a) / 255);
                            }

                            redoLine[0] = tgtLine[0];
                            redoLine[1] = tgtLine[1];
                            redoLine[2] = tgtLine[2];
                            redoLine[3] = tgtLine[3];

                            srcLine += srcBpp;
                            tgtLine += 4;
                            redoLine += 4;
                            undoLine += 4;
                            if (swapLine != null)
                                swapLine += 4;
                        }
                    }
                }
            }
            target.AddDirtyRect(new PixelRect(posX, posY, sWidth, sHeight));
            SwapBitmap?.AddDirtyRect(new PixelRect(0, 0, sWidth, sHeight));

            SwapBitmap?.Unlock();
            target.Unlock();
            source.Unlock();
        }

        public void FillRectBitmapNoConvert(
            IWriteableBitmap source, 
            IWriteableBitmap target, 
            (int X, int Y) pos)
        {
            int bytesPerPixel = source.HasAlpha ? 4 : 3;

            if (target.HasAlpha != source.HasAlpha)
                throw new ArgumentException("Target and source formats must be the same.");

            int sWidth = source.PixelWidth;
            int sHeight = source.PixelHeight;

            int tWidth = target.PixelWidth;
            int tHeight = target.PixelHeight;

            int posX = Math.Max(0, pos.X);
            int posY = Math.Max(0, pos.Y);

            sWidth = Math.Min(sWidth, tWidth - posX);
            sHeight = Math.Min(sHeight, tHeight - posY);

            if (sWidth <= 0 || sHeight <= 0)
                return;

            int sourceStride = source.BackBufferStride;
            int targetStride = target.BackBufferStride;
            int strideDelta = targetStride - sourceStride;

            source.Lock();
            target.Lock();

            unsafe
            {
                byte* sourcePtr = (byte*)source.BackBuffer;
                byte* targetPtr = (byte*)target.BackBuffer;

                targetPtr += posY * targetStride + posX * bytesPerPixel;

                for (int r = 0; r < sHeight; r++)
                {
                    for (int s = 0; s < sourceStride; s++)
                    {
                        *targetPtr++ = *sourcePtr++;
                    }
                    targetPtr += strideDelta;
                }
            }
            target.AddDirtyRect(new PixelRect(pos.X, pos.Y, sWidth, sHeight));
            source.Unlock();
            target.Unlock();
        }
    }
}
