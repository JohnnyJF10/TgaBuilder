using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        public void FillRectBitmapUnmonitored(
            IWriteableBitmap source,
            IWriteableBitmap target,
            (int X, int Y) pos,
            double opacity = 1.0,
            PlacingMode placingMode = PlacingMode.Default)
        {
            if (target.HasAlpha)
                FillRectBitmap32Unmonitored(source, target, pos, opacity, placingMode);
            else
                FillRectBitmap24Unmonitored(source, target, pos, opacity, placingMode);
        }

        private void FillRectBitmap24Unmonitored(
            IWriteableBitmap source,
            IWriteableBitmap target,
            (int X, int Y) pos,
            double opacity = 1.0,
            PlacingMode placingMode = PlacingMode.Default)
        {
            int sWidth = source.PixelWidth;
            int sHeight = source.PixelHeight;

            int tWidth = target.PixelWidth;
            int tHeight = target.PixelHeight;

            int posX = Math.Max(0, pos.X);
            int posY = Math.Max(0, pos.Y);

            sWidth = Math.Min(sWidth, tWidth - posX);
            sHeight = Math.Min(sHeight, tHeight - posY);

            if (sWidth <= 0 || sHeight <= 0)
                throw new ArgumentException("Source bitmap dimensions must be greater than zero.");

            bool OverlayTransparent =
                (placingMode & PlacingMode.OverlayTransparent) == PlacingMode.OverlayTransparent;

            bool SwapAndPlace =
                (placingMode & PlacingMode.PlaceAndSwap) == PlacingMode.PlaceAndSwap;

            if (SwapAndPlace && SwapBitmap is null)
                throw new ArgumentException("SwapBitmap must be set when OverlayTransparent is used.");

            PixelAction action;
            bool isTransparencyColor;

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

                        if (swapLine != null)
                        {
                            swapLine[0] = tgtLine[0];
                            swapLine[1] = tgtLine[1];
                            swapLine[2] = tgtLine[2];
                        }

                        a = (byte)(a * opacity);
                        isTransparencyColor = (r, g, b) == (255, 0, 255);

                        action = DecidePixelAction(
                            alpha:                  a,
                            srcHasAlpha:            source.HasAlpha,
                            tgtHasAlpha:            false,
                            isTransparencyColor:    isTransparencyColor,
                            overlayTransparent:     OverlayTransparent);

                        switch (action)
                        {
                            case PixelAction.Copy:
                                tgtLine[0] = r;
                                tgtLine[1] = g;
                                tgtLine[2] = b;
                                break;
                            case PixelAction.Transparent:
                                tgtLine[0] = 255;
                                tgtLine[1] = 0;
                                tgtLine[2] = 255;
                                break;
                            case PixelAction.Blend:
                                tgtLine[0] = DoAlphaBlend(b, tgtLine[0], a);
                                tgtLine[1] = DoAlphaBlend(g, tgtLine[1], a);
                                tgtLine[2] = DoAlphaBlend(r, tgtLine[2], a);
                                break;
                            case PixelAction.None:
                            default:
                                break;
                        }

                        srcLine += srcBpp;
                        tgtLine += 3;
                        if (swapLine != null)
                            swapLine += 3;
                    }
                }
            }
            target.AddDirtyRect(new PixelRect(posX, posY, sWidth, sHeight));
            SwapBitmap?.AddDirtyRect(new PixelRect(0, 0, sWidth, sHeight));

            SwapBitmap?.Unlock();
            target.Unlock();
            source.Unlock();
        }

        private void FillRectBitmap32Unmonitored(
            IWriteableBitmap source,
            IWriteableBitmap target,
            (int X, int Y) pos,
            double opacity = 1.0,
            PlacingMode placingMode = PlacingMode.Default)
        {
            int sWidth = source.PixelWidth;
            int sHeight = source.PixelHeight;

            int tWidth = target.PixelWidth;
            int tHeight = target.PixelHeight;

            int posX = Math.Max(0, pos.X);
            int posY = Math.Max(0, pos.Y);

            sWidth = Math.Min(sWidth, tWidth - posX);
            sHeight = Math.Min(sHeight, tHeight - posY);

            if (sWidth <= 0 || sHeight <= 0)
                throw new ArgumentException("Source bitmap dimensions must be greater than zero.");

            bool OverlayTransparent =
                (placingMode & PlacingMode.OverlayTransparent) == PlacingMode.OverlayTransparent;

            bool SwapAndPlace =
                (placingMode & PlacingMode.PlaceAndSwap) == PlacingMode.PlaceAndSwap;

            if (SwapAndPlace && SwapBitmap is null)
                throw new ArgumentException("SwapBitmap must be set when OverlayTransparent is used.");

            PixelAction action;
            bool isTransparencyColor;

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

                        if (swapLine != null)
                        {
                            swapLine[0] = tgtLine[0];
                            swapLine[1] = tgtLine[1];
                            swapLine[2] = tgtLine[2];
                            swapLine[3] = tgtLine[3];
                        }

                        a = (byte)(a * opacity);
                        isTransparencyColor = (r, g, b) == (255, 0, 255);

                        action = DecidePixelAction(
                            alpha:                  a,
                            srcHasAlpha:            source.HasAlpha,
                            tgtHasAlpha:            true,
                            isTransparencyColor:    isTransparencyColor,
                            overlayTransparent:     OverlayTransparent);

                        switch (action)
                        {
                            case PixelAction.Copy:
                                tgtLine[0] = b;
                                tgtLine[1] = g;
                                tgtLine[2] = r;
                                tgtLine[3] = a;
                                break;
                            case PixelAction.Transparent:
                                tgtLine[0] = 0;
                                tgtLine[1] = 0;
                                tgtLine[2] = 0;
                                tgtLine[3] = 0;
                                break;
                            case PixelAction.Blend:
                                tgtLine[0] = DoAlphaBlend(b, tgtLine[0], a);
                                tgtLine[1] = DoAlphaBlend(g, tgtLine[1], a);
                                tgtLine[2] = DoAlphaBlend(r, tgtLine[2], a);
                                break;
                            case PixelAction.None:
                            default:
                                break;
                        }

                        srcLine += srcBpp;
                        tgtLine += 4;
                        if (swapLine != null)
                            swapLine += 4;
                    }
                }
            }
            target.AddDirtyRect(new PixelRect(posX, posY, sWidth, sHeight));
            SwapBitmap?.AddDirtyRect(new PixelRect(0, 0, sWidth, sHeight));

            SwapBitmap?.Unlock();
            target.Unlock();
            source.Unlock();
        }
    }
}
