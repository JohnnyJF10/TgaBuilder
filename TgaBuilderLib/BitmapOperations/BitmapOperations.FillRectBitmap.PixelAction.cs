using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations
    {
        private enum PixelAction
        {
            Copy,           // Direct copy
            Transparent,    // Make transparent
            Blend,          // Alpha blend
            None            // Retain original
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte DoAlphaBlend(byte src, byte tgt, byte alpha)
            => (byte)(((tgt * (255 - alpha)) + (src * alpha)) / 255);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PixelAction DecidePixelAction(
            int alpha, 
            bool srcHasAlpha, 
            bool tgtHasAlpha, 
            bool isTransparencyColor, 
            bool overlayTransparent)
        {
            // -------------------------------------------
            // Copy
            // -------------------------------------------
            if (tgtHasAlpha)
            {
                if (!overlayTransparent)
                {
                    if (srcHasAlpha || !isTransparencyColor)
                    {
                        return PixelAction.Copy;
                    }
                }
            }
            else // tgtHasAlpha == false
            {
                if (alpha == 255)
                {
                    if (!overlayTransparent || !isTransparencyColor)
                    {
                        return PixelAction.Copy;
                    }
                }
            }

            // -------------------------------------------
            // Transparent
            // -------------------------------------------
            if (!overlayTransparent)
            {
                if (tgtHasAlpha)
                {
                    if (srcHasAlpha)
                    {
                        if (alpha == 0)
                        {
                            return PixelAction.Transparent;
                        }
                    }
                    else // srcHasAlpha == false
                    {
                        if (isTransparencyColor)
                        {
                            return PixelAction.Transparent;
                        }
                    }
                }
                else // tgtHasAlpha == false
                {
                    if (alpha == 0)
                    {
                        return PixelAction.Transparent;
                    }
                    if (srcHasAlpha && isTransparencyColor)
                    {
                        return PixelAction.Transparent;
                    }
                }
            }

            // -------------------------------------------
            // Blend
            // -------------------------------------------
            if (srcHasAlpha || !isTransparencyColor)
            {
                if (tgtHasAlpha)
                {
                    return PixelAction.Blend;
                }
                else // tgtHasAlpha == false
                {
                    if (alpha < 255)
                    {
                        return PixelAction.Blend;
                    }
                }
            }

            // -------------------------------------------
            // None
            // -------------------------------------------
            return PixelAction.None;
        }

    }
}
