using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    private byte[] BricksDraw(byte[] tilePixels, byte[] bgPixels, bool[] selection)
    {
        if (bgPixels.Length != tilePixels.Length)
            throw new ArgumentException("Input image raw arrays must have same length.");

        if (bgPixels.Length != selection.Length * TRANSITIONS_BPP)
            throw new ArgumentException("Input arrays length must match dimensions.");

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

                // Pre-calculate alpha values for blending
                int eA = EdgeColor.A ?? 255;      // Edge alpha (0-255)
                int invA = 255 - eA;              // Inverse alpha for background contribution
                int eR = EdgeColor.R;
                int eG = EdgeColor.G;
                int eB = EdgeColor.B;

                for (int y = 0; y < Height; y++)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < Width; x++)
                    {
                        int pixelIndex = y * Width + x;
                        if (!selection[pixelIndex]) continue;

                        int offset = rowOffset + (x * 4);

                        // Initial step: copy selected pixel from tile to result
                        // (Already done for non-edge pixels by logic below or by a full copy)
                        // For simplicity and clarity, we handle the edge case specifically:

                        if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                        {
                            pRes[offset + 0] = pTile[offset + 0];
                            pRes[offset + 1] = pTile[offset + 1];
                            pRes[offset + 2] = pTile[offset + 2];
                            pRes[offset + 3] = pTile[offset + 3];
                            continue;
                        }

                        bool isEdge = !selection[pixelIndex - 1] || !selection[pixelIndex + 1] ||
                                      !selection[pixelIndex - Width] || !selection[pixelIndex + Width];

                        if (isEdge)
                        {
                            // 1. Tint the tile pixel color with EdgeColor
                            // 2. Alpha blend the result with the background

                            // Blue
                            int tintedB = (pTile[offset + 0] * eB) / 255;
                            pRes[offset + 0] = (byte)((tintedB * eA + pBg[offset + 0] * invA) / 255);

                            // Green
                            int tintedG = (pTile[offset + 1] * eG) / 255;
                            pRes[offset + 1] = (byte)((tintedG * eA + pBg[offset + 1] * invA) / 255);

                            // Red
                            int tintedR = (pTile[offset + 2] * eR) / 255;
                            pRes[offset + 2] = (byte)((tintedR * eA + pBg[offset + 2] * invA) / 255);

                            // Alpha (Blending the transparency)
                            pRes[offset + 3] = (byte)((pTile[offset + 3] * eA + pBg[offset + 3] * invA) / 255);
                        }
                        else
                        {
                            // Standard selection: just copy from tile
                            pRes[offset + 0] = pTile[offset + 0];
                            pRes[offset + 1] = pTile[offset + 1];
                            pRes[offset + 2] = pTile[offset + 2];
                            pRes[offset + 3] = pTile[offset + 3];
                        }
                    }
                }
            }
        }

        return result;
    }
}
