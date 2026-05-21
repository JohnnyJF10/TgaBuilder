using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{

    public enum EdgeBlendMode
    {
        Multiply,       // Multiply (Darkens, like a glaze)
        Screen,         // Multiply negatively (Lightens, like projection)
        Additive,       // Add (Extremely lightens, glow effect)
        Overlay,        // Copy into each other (Enhances contrast)
        HardLight,      // Hard light (Strong effect)
        SoftLight,      // Soft light (Smooth contrast effect)
        ColorDodge,     // Brightens strongly with edge color
        ColorBurn       // Darkens strongly with edge color
    }

    private byte[] BricksDraw(byte[] tilePixels, byte[] bgPixels, bool[] selection)
    {
        static int SoftLightChannel(int tileChannel, int edgeChannel)
        {
            double tileNorm = tileChannel / 255.0;
            double edgeNorm = edgeChannel / 255.0;
            double blended = ((1.0 - (2.0 * edgeNorm)) * tileNorm * tileNorm) + (2.0 * edgeNorm * tileNorm);
            return (int)Math.Clamp(blended * 255.0, 0, 255);
        }

        if (bgPixels.Length != tilePixels.Length)
            throw new ArgumentException("Input image raw arrays must have same length.");

        if (bgPixels.Length != selection.Length * TRANSITIONS_BPP)
            throw new ArgumentException("Input arrays length must match dimensions.");

        // Clamp the maximum edge width to the range 0 to 12
        EdgeWidth = Math.Clamp(EdgeWidth, 0, 12);
        ShadowSize = Math.Clamp(ShadowSize, 0, 32);
        ShadowHardness = Math.Clamp(ShadowHardness, 0, 100);

        int stride = Width * TRANSITIONS_BPP;
        var result = new byte[bgPixels.Length];

        unsafe
        {
            fixed (byte* pBg = bgPixels)
            fixed (byte* pTile = tilePixels)
            fixed (byte* pRes = result)
            {
                // Copy the background first
                Buffer.MemoryCopy(pBg, pRes, Height * stride, Height * stride);

                // Pre-calculate alpha and color values for basic edge blending
                int eA = EdgeColor.A ?? 255;      // Edge alpha (0-255)
                int invA = 255 - eA;              // Inverse alpha for background contribution
                int eR = EdgeColor.R;
                int eG = EdgeColor.G;
                int eB = EdgeColor.B;
                int sA = ShadowColor.A ?? 255;
                int sR = ShadowColor.R;
                int sG = ShadowColor.G;
                int sB = ShadowColor.B;
                double shadowHardnessPower = 1.0 + (ShadowHardness / 100.0) * 4.0;

                for (int y = 0; y < Height; y++)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < Width; x++)
                    {
                        int pixelIndex = y * Width + x;
                        int offset = rowOffset + (x * 4);

                        // 1. Calculate dynamic widths based on proximity to image borders.
                        int distToBorderX = Math.Min(x, Width - 1 - x);
                        int distToBorderY = Math.Min(y, Height - 1 - y);
                        int distToBorder = Math.Min(distToBorderX, distToBorderY);

                        // Dynamic widths drop linearly towards image bounds.
                        int dynamicEdgeWidth = Math.Min(EdgeWidth, distToBorder);
                        int dynamicShadowSize = Math.Min(ShadowSize, distToBorder);

                        if (selection[pixelIndex])
                        {
                            int minDist = dynamicEdgeWidth + 1;

                            // 2. Find the shortest distance to the next unselected pixel
                            if (dynamicEdgeWidth > 0)
                            {
                                // Ring-like search outwards up to the 'dynamicEdgeWidth'
                                for (int d = 1; d <= dynamicEdgeWidth; d++)
                                {
                                    bool foundEdge = false;

                                    // Check the perimeter of the square at distance 'd'
                                    for (int i = -d; i <= d; i++)
                                    {
                                        // Top and Bottom edges of the search square
                                        int topY = y - d;
                                        int botY = y + d;
                                        int xPlusI = x + i;

                                        // Check top boundary (out of bounds logic kept for safety,
                                        // though dynamicEdgeWidth theoretically prevents it)
                                        if (topY < 0 || topY >= Height || xPlusI < 0 || xPlusI >= Width || !selection[topY * Width + xPlusI])
                                            foundEdge = true;
                                        // Check bottom boundary
                                        else if (botY < 0 || botY >= Height || xPlusI < 0 || xPlusI >= Width || !selection[botY * Width + xPlusI])
                                            foundEdge = true;

                                        // Left and Right edges (skip corners to avoid duplicate checks)
                                        int leftX = x - d;
                                        int rightX = x + d;
                                        int yPlusI = y + i;
                                        if (i > -d && i < d)
                                        {
                                            if (leftX < 0 || leftX >= Width || yPlusI < 0 || yPlusI >= Height || !selection[yPlusI * Width + leftX])
                                                foundEdge = true;
                                            else if (rightX < 0 || rightX >= Width || yPlusI < 0 || yPlusI >= Height || !selection[yPlusI * Width + rightX])
                                                foundEdge = true;
                                        }

                                        if (foundEdge) break;
                                    }

                                    if (foundEdge)
                                    {
                                        minDist = d;
                                        break; // Found the closest edge, stop searching
                                    }
                                }
                            }

                            // 3. Color the selected tile pixel based on the distance (Edge tint)
                            if (dynamicEdgeWidth > 0 && minDist <= dynamicEdgeWidth)
                            {
                                int weight255 = ((dynamicEdgeWidth - minDist + 1) * 255) / dynamicEdgeWidth;
                                int invWeight255 = 255 - weight255;

                                // Current channels of the tile pixel
                                int tB = pTile[offset + 0];
                                int tG = pTile[offset + 1];
                                int tR = pTile[offset + 2];
                                int tA = pTile[offset + 3];

                                int tintedB, tintedG, tintedR;

                                // 1. Application mode for the edge color
                                switch (BlendMode)
                                {
                                    case EdgeBlendMode.Screen: // Multiply negatively
                                        tintedB = 255 - ((255 - tB) * (255 - eB) / 255);
                                        tintedG = 255 - ((255 - tG) * (255 - eG) / 255);
                                        tintedR = 255 - ((255 - tR) * (255 - eR) / 255);
                                        break;

                                    case EdgeBlendMode.Additive: // Add
                                        tintedB = Math.Min(255, tB + eB);
                                        tintedG = Math.Min(255, tG + eG);
                                        tintedR = Math.Min(255, tR + eR);
                                        break;

                                    case EdgeBlendMode.Overlay: // Copy into each other
                                        tintedB = (tB < 128) ? (2 * tB * eB / 255) : (255 - 2 * (255 - tB) * (255 - eB) / 255);
                                        tintedG = (tG < 128) ? (2 * tG * eG / 255) : (255 - 2 * (255 - tG) * (255 - eG) / 255);
                                        tintedR = (tR < 128) ? (2 * tR * eR / 255) : (255 - 2 * (255 - tR) * (255 - eR) / 255);
                                        break;

                                    case EdgeBlendMode.HardLight:
                                        tintedB = (eB < 128) ? (2 * tB * eB / 255) : (255 - 2 * (255 - tB) * (255 - eB) / 255);
                                        tintedG = (eG < 128) ? (2 * tG * eG / 255) : (255 - 2 * (255 - tG) * (255 - eG) / 255);
                                        tintedR = (eR < 128) ? (2 * tR * eR / 255) : (255 - 2 * (255 - tR) * (255 - eR) / 255);
                                        break;

                                    case EdgeBlendMode.SoftLight:
                                        tintedB = SoftLightChannel(tB, eB);
                                        tintedG = SoftLightChannel(tG, eG);
                                        tintedR = SoftLightChannel(tR, eR);
                                        break;

                                    case EdgeBlendMode.ColorDodge:
                                        tintedB = eB == 255 ? 255 : Math.Min(255, (tB * 255) / (255 - eB));
                                        tintedG = eG == 255 ? 255 : Math.Min(255, (tG * 255) / (255 - eG));
                                        tintedR = eR == 255 ? 255 : Math.Min(255, (tR * 255) / (255 - eR));
                                        break;

                                    case EdgeBlendMode.ColorBurn:
                                        tintedB = eB == 0 ? 0 : Math.Max(0, 255 - ((255 - tB) * 255) / eB);
                                        tintedG = eG == 0 ? 0 : Math.Max(0, 255 - ((255 - tG) * 255) / eG);
                                        tintedR = eR == 0 ? 0 : Math.Max(0, 255 - ((255 - tR) * 255) / eR);
                                        break;

                                    case EdgeBlendMode.Multiply: // Standard: Multiply
                                    default:
                                        tintedB = (tB * eB) / 255;
                                        tintedG = (tG * eG) / 255;
                                        tintedR = (tR * eR) / 255;
                                        break;
                                }

                                // 2. Background influence
                                // How strongly does the edge color influence the original background?
                                int maxEdgeB = (tintedB * eA + pBg[offset + 0] * invA) / 255;
                                int maxEdgeG = (tintedG * eA + pBg[offset + 1] * invA) / 255;
                                int maxEdgeR = (tintedR * eA + pBg[offset + 2] * invA) / 255;
                                int maxEdgeAlpha = (tA * eA + pBg[offset + 3] * invA) / 255;

                                // 3. Final gradient blending based on distance to edge
                                pRes[offset + 0] = (byte)((maxEdgeB * weight255 + tB * invWeight255) / 255);
                                pRes[offset + 1] = (byte)((maxEdgeG * weight255 + tG * invWeight255) / 255);
                                pRes[offset + 2] = (byte)((maxEdgeR * weight255 + tR * invWeight255) / 255);
                                pRes[offset + 3] = (byte)((maxEdgeAlpha * weight255 + tA * invWeight255) / 255);
                            }
                            else
                            {
                                // Inner pixels or absolute image border pixels: copy original tile.
                                pRes[offset + 0] = pTile[offset + 0];
                                pRes[offset + 1] = pTile[offset + 1];
                                pRes[offset + 2] = pTile[offset + 2];
                                pRes[offset + 3] = pTile[offset + 3];
                            }
                        }
                        else if (dynamicShadowSize > 0)
                        {
                            // Draw shadow only on background pixels close to the selection edge.
                            int minShadowDist = dynamicShadowSize + 1;

                            for (int d = 1; d <= dynamicShadowSize; d++)
                            {
                                bool foundSelectedNeighbor = false;

                                for (int i = -d; i <= d; i++)
                                {
                                    int topY = y - d;
                                    int botY = y + d;
                                    int xPlusI = x + i;

                                    if (topY >= 0 && topY < Height && xPlusI >= 0 && xPlusI < Width && selection[topY * Width + xPlusI])
                                        foundSelectedNeighbor = true;
                                    if (!foundSelectedNeighbor && botY >= 0 && botY < Height && xPlusI >= 0 && xPlusI < Width && selection[botY * Width + xPlusI])
                                        foundSelectedNeighbor = true;

                                    int leftX = x - d;
                                    int rightX = x + d;
                                    int yPlusI = y + i;
                                    if (!foundSelectedNeighbor && i > -d && i < d)
                                    {
                                        if (leftX >= 0 && leftX < Width && yPlusI >= 0 && yPlusI < Height && selection[yPlusI * Width + leftX])
                                            foundSelectedNeighbor = true;
                                        else if (rightX >= 0 && rightX < Width && yPlusI >= 0 && yPlusI < Height && selection[yPlusI * Width + rightX])
                                            foundSelectedNeighbor = true;
                                    }

                                    if (foundSelectedNeighbor) break;
                                }

                                if (foundSelectedNeighbor)
                                {
                                    minShadowDist = d;
                                    break;
                                }
                            }

                            if (minShadowDist <= dynamicShadowSize)
                            {
                                double proximity = (double)(dynamicShadowSize - minShadowDist + 1) / dynamicShadowSize;
                                int shadowWeight255 = (int)Math.Clamp(Math.Round(Math.Pow(proximity, shadowHardnessPower) * sA), 0, 255);
                                int invShadowWeight255 = 255 - shadowWeight255;

                                pRes[offset + 0] = (byte)((sB * shadowWeight255 + pBg[offset + 0] * invShadowWeight255) / 255);
                                pRes[offset + 1] = (byte)((sG * shadowWeight255 + pBg[offset + 1] * invShadowWeight255) / 255);
                                pRes[offset + 2] = (byte)((sR * shadowWeight255 + pBg[offset + 2] * invShadowWeight255) / 255);
                                pRes[offset + 3] = (byte)((255 * shadowWeight255 + pBg[offset + 3] * invShadowWeight255) / 255);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
}
