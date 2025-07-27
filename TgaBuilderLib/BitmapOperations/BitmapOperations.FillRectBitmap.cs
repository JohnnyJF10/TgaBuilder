﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public WriteableBitmap? SwapBitmap { get; set; }

        public int PlacedSize { get; set; }

        private (byte tr, byte tg, byte tb) _transparentColor = (255, 0, 255);

        public void FillRectBitmap(
            WriteableBitmap source, 
            WriteableBitmap target,
            (int X, int Y) pos, 
            byte[] undoPixels, 
            byte[] redoPixels,
            PlacingMode placingMode = PlacingMode.Default)
        {
            if (source.Format != PixelFormats.Rgb24 && source.Format != PixelFormats.Bgra32)
                throw new ArgumentException("Source must be in Rgb24 or Bgra32 format.");

            if (target.Format == PixelFormats.Rgb24)
                FillRectBitmap24(source, target, pos, undoPixels, redoPixels, placingMode);
            else if (target.Format == PixelFormats.Bgra32)
                FillRectBitmap32(source, target, pos, undoPixels, redoPixels, placingMode);
            else
                throw new ArgumentException("Target must be in Rgb24 or Bgra32 format.");


        }

        private void FillRectBitmap24(
            WriteableBitmap source,
            WriteableBitmap target,
            (int X, int Y) pos,
            byte[] undoPixels,
            byte[] redoPixels,
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
                int srcBpp = (source.Format.BitsPerPixel + 7) / 8;

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

                            if (source.Format == PixelFormats.Rgb24)
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
                            undoLine[0] = tgtLine[0];
                            undoLine[1] = tgtLine[1];
                            undoLine[2] = tgtLine[2];

                            if (swapLine != null)
                            {
                                swapLine[0] = tgtLine[0];
                                swapLine[1] = tgtLine[1];
                                swapLine[2] = tgtLine[2];
                            }

                            if ((srcBpp == 3 && (!OverlayTransparent || (r, g, b) != _transparentColor)) || (srcBpp > 3 && a == 255))
                            {
                                tgtLine[0] = r;
                                tgtLine[1] = g;
                                tgtLine[2] = b;
                            }
                            else if (a == 0 && !OverlayTransparent)
                            {
                                tgtLine[0] = _transparentColor.tr;
                                tgtLine[1] = _transparentColor.tg;
                                tgtLine[2] = _transparentColor.tb;
                            }
                            else if (a < 255)
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
            target.AddDirtyRect(new Int32Rect(posX, posY, sWidth, sHeight));
            SwapBitmap?.AddDirtyRect(new Int32Rect(0, 0, sWidth, sHeight));

            SwapBitmap?.Unlock();
            target.Unlock();
            source.Unlock();
        }

        private void FillRectBitmap32(
            WriteableBitmap source,
            WriteableBitmap target,
            (int X, int Y) pos,
            byte[] undoPixels,
            byte[] redoPixels,
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
                int srcBpp = (source.Format.BitsPerPixel + 7) / 8;

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

                            if (source.Format == PixelFormats.Rgb24)
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

                            if ((srcBpp == 3 && ((r, g, b) != _transparentColor)) || (srcBpp > 3 && !OverlayTransparent))
                            {
                                tgtLine[0] = b;
                                tgtLine[1] = g;
                                tgtLine[2] = r;
                                tgtLine[3] = a;
                            }
                            else if (srcBpp == 3 && (r, g, b) == _transparentColor && !OverlayTransparent)
                            {
                                tgtLine[0] = 0;
                                tgtLine[1] = 0;
                                tgtLine[2] = 0;
                                tgtLine[3] = 0;
                            }
                            else if (srcBpp > 3 && OverlayTransparent)
                            {
                                tgtLine[0] = (byte)((tgtLine[0] * (255 - a) + b * a) / 255);
                                tgtLine[1] = (byte)((tgtLine[1] * (255 - a) + g * a) / 255);
                                tgtLine[2] = (byte)((tgtLine[2] * (255 - a) + r * a) / 255);
                                tgtLine[3] = (byte)((tgtLine[3] * (255 - a) + a * a) / 255);
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
            target.AddDirtyRect(new Int32Rect(posX, posY, sWidth, sHeight));
            SwapBitmap?.AddDirtyRect(new Int32Rect(0, 0, sWidth, sHeight));

            SwapBitmap?.Unlock();
            target.Unlock();
            source.Unlock();
        }

        public void FillRectBitmapNoConvert(
            WriteableBitmap source, 
            WriteableBitmap target, 
            (int X, int Y) pos)
        {
            int bytesPerPixel = (source.Format.BitsPerPixel + 7) >> 3;
            if (target.Format.BitsPerPixel != source.Format.BitsPerPixel)
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
            target.AddDirtyRect(new Int32Rect(pos.X, pos.Y, sWidth, sHeight));
            source.Unlock();
            target.Unlock();
        }
    }
}
