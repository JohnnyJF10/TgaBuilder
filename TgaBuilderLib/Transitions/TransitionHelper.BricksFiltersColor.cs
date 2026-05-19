using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    public unsafe void BoxBlurColor(byte[] blur, byte[] input)
    {
        int width = Width;
        int height = Height;
        int stride = width * 4;

        fixed (byte* pInput = input, pBlur = blur)
        {
            for (int y = 1; y < height - 1; y++)
            {
                int row = y * stride;

                byte* pCurr = pInput + row;
                byte* pPrev = pCurr - stride;
                byte* pNext = pCurr + stride;
                byte* pOut = pBlur + row;

                for (int x = 1; x < width - 1; x++)
                {
                    int idx = x * 4;
                    int lIdx = idx - 4; // Left pixel byte index
                    int rIdx = idx + 4; // Right pixel byte index

                    // Process Blue (0), Green (1), and Red (2) channels independently
                    pOut[idx] = (byte)((
                        pPrev[lIdx] + pPrev[idx] + pPrev[rIdx] +
                        pCurr[lIdx] + pCurr[idx] + pCurr[rIdx] +
                        pNext[lIdx] + pNext[idx] + pNext[rIdx]
                    ) * 0.11111111f);

                    pOut[idx + 1] = (byte)((
                        pPrev[lIdx + 1] + pPrev[idx + 1] + pPrev[rIdx + 1] +
                        pCurr[lIdx + 1] + pCurr[idx + 1] + pCurr[rIdx + 1] +
                        pNext[lIdx + 1] + pNext[idx + 1] + pNext[rIdx + 1]
                    ) * 0.11111111f);

                    pOut[idx + 2] = (byte)((
                        pPrev[lIdx + 2] + pPrev[idx + 2] + pPrev[rIdx + 2] +
                        pCurr[lIdx + 2] + pCurr[idx + 2] + pCurr[rIdx + 2] +
                        pNext[lIdx + 2] + pNext[idx + 2] + pNext[rIdx + 2]
                    ) * 0.11111111f);

                    // Alpha (3) - Pass-through unaltered
                    pOut[idx + 3] = pCurr[idx + 3];
                }
            }
        }
    }

    public unsafe void MedianFilter3x3Color(byte[] output, byte[] input)
    {
        int width = Width;
        int height = Height;
        int stride = width * 4;

        fixed (byte* pIn = input, pOut = output)
        {
            for (int y = 1; y < height - 1; y++)
            {
                int row = y * stride;
                byte* pCurr = pIn + row;
                byte* pPrev = pCurr - stride;
                byte* pNext = pCurr + stride;
                byte* pDst = pOut + row;

                for (int x = 1; x < width - 1; x++)
                {
                    int idx = x * 4;
                    int lIdx = idx - 4;
                    int rIdx = idx + 4;

                    // Execute sorting network for color channels (B, G, R)
                    for (int c = 0; c < 3; c++)
                    {
                        byte v0 = pPrev[lIdx + c], v1 = pPrev[idx + c], v2 = pPrev[rIdx + c];
                        byte v3 = pCurr[lIdx + c], v4 = pCurr[idx + c], v5 = pCurr[rIdx + c];
                        byte v6 = pNext[lIdx + c], v7 = pNext[idx + c], v8 = pNext[rIdx + c];

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        void Swap(ref byte a, ref byte b)
                        {
                            if (a > b)
                            {
                                byte t = a;
                                a = b;
                                b = t;
                            }
                        }

                        Swap(ref v1, ref v2); Swap(ref v4, ref v5); Swap(ref v7, ref v8);
                        Swap(ref v0, ref v1); Swap(ref v3, ref v4); Swap(ref v6, ref v7);
                        Swap(ref v1, ref v2); Swap(ref v4, ref v5); Swap(ref v7, ref v8);
                        Swap(ref v0, ref v3); Swap(ref v5, ref v8); Swap(ref v4, ref v7);
                        Swap(ref v3, ref v6); Swap(ref v1, ref v4); Swap(ref v2, ref v5);
                        Swap(ref v4, ref v7); Swap(ref v4, ref v2); Swap(ref v6, ref v4);
                        Swap(ref v4, ref v2);

                        pDst[idx + c] = v4;
                    }

                    // Preserve Alpha channel
                    pDst[idx + 3] = pCurr[idx + 3];
                }
            }
        }
    }

    public unsafe void BilateralFilter3x3Color(byte[] output, byte[] input, float sigma = 0.1f)
    {
        float safeSigma = Math.Max(sigma, 0.001f);
        float twoSigmaSq = 2f * safeSigma * safeSigma;

        int width = Width;
        int height = Height;
        int stride = width * 4;

        fixed (byte* pIn = input, pOut = output)
        {
            // Spatial element offsets calculation converted to byte strides
            int* offsets = stackalloc int[9] {
                -stride - 4, -stride, -stride + 4,
                -4,          0,       4,
                 stride - 4,  stride,  stride + 4
            };

            for (int y = 1; y < height - 1; y++)
            {
                int rowOffset = y * stride;
                byte* srcCenter = pIn + rowOffset;
                byte* dstCenter = pOut + rowOffset;

                for (int x = 1; x < width - 1; x++)
                {
                    int idx = x * 4;

                    byte centerB = srcCenter[idx];
                    byte centerG = srcCenter[idx + 1];
                    byte centerR = srcCenter[idx + 2];

                    float sumWeight = 0f;
                    float sumB = 0f;
                    float sumG = 0f;
                    float sumR = 0f;

                    for (int i = 0; i < 9; i++)
                    {
                        byte* pNeighbor = srcCenter + idx + offsets[i];
                        byte nB = pNeighbor[0];
                        byte nG = pNeighbor[1];
                        byte nR = pNeighbor[2];

                        float diffB = (centerB - nB) / 255f;
                        float diffG = (centerG - nG) / 255f;
                        float diffR = (centerR - nR) / 255f;

                        // Euclidean distance squared in normalized BGR color space
                        float diffSq = (diffB * diffB) + (diffG * diffG) + (diffR * diffR);
                        float weight = (float)Math.Exp(-diffSq / twoSigmaSq);

                        sumB += nB * weight;
                        sumG += nG * weight;
                        sumR += nR * weight;
                        sumWeight += weight;
                    }

                    dstCenter[idx] = (byte)Math.Clamp(sumB / sumWeight, 0f, 255f);
                    dstCenter[idx + 1] = (byte)Math.Clamp(sumG / sumWeight, 0f, 255f);
                    dstCenter[idx + 2] = (byte)Math.Clamp(sumR / sumWeight, 0f, 255f);
                    dstCenter[idx + 3] = srcCenter[idx + 3]; // Preserve Alpha
                }
            }
        }
    }

    public unsafe void GaussianBlur3x3Color(byte[] output, byte[] input, float sigma = 1.0f)
    {
        float* kernel = stackalloc float[9];
        float kernelSum = 0f;
        float twoSigmaSq = 2f * sigma * sigma;

        int kIdx = 0;
        for (int ky = -1; ky <= 1; ky++)
        {
            for (int kx = -1; kx <= 1; kx++)
            {
                float distSq = (kx * kx) + (ky * ky);
                float weight = (float)Math.Exp(-distSq / twoSigmaSq);

                kernel[kIdx++] = weight;
                kernelSum += weight;
            }
        }

        for (int i = 0; i < 9; i++)
        {
            kernel[i] /= kernelSum;
        }

        int width = Width;
        int height = Height;
        int stride = width * 4;

        fixed (byte* pIn = input, pOut = output)
        {
            for (int y = 1; y < height - 1; y++)
            {
                int rowOffset = y * stride;

                byte* srcCenter = pIn + rowOffset;
                byte* dstCenter = pOut + rowOffset;

                for (int x = 1; x < width - 1; x++)
                {
                    int idx = x * 4;

                    // Accumulate Blue
                    float sumB =
                        srcCenter[idx - stride - 4] * kernel[0] + srcCenter[idx - stride] * kernel[1] + srcCenter[idx - stride + 4] * kernel[2] +
                        srcCenter[idx - 4] * kernel[3] + srcCenter[idx] * kernel[4] + srcCenter[idx + 4] * kernel[5] +
                        srcCenter[idx + stride - 4] * kernel[6] + srcCenter[idx + stride] * kernel[7] + srcCenter[idx + stride + 4] * kernel[8];

                    // Accumulate Green
                    float sumG =
                        srcCenter[idx - stride - 4 + 1] * kernel[0] + srcCenter[idx - stride + 1] * kernel[1] + srcCenter[idx - stride + 4 + 1] * kernel[2] +
                        srcCenter[idx - 4 + 1] * kernel[3] + srcCenter[idx + 1] * kernel[4] + srcCenter[idx + 4 + 1] * kernel[5] +
                        srcCenter[idx + stride - 4 + 1] * kernel[6] + srcCenter[idx + stride + 1] * kernel[7] + srcCenter[idx + stride + 4 + 1] * kernel[8];

                    // Accumulate Red
                    float sumR =
                        srcCenter[idx - stride - 4 + 2] * kernel[0] + srcCenter[idx - stride + 2] * kernel[1] + srcCenter[idx - stride + 4 + 2] * kernel[2] +
                        srcCenter[idx - 4 + 2] * kernel[3] + srcCenter[idx + 2] * kernel[4] + srcCenter[idx + 4 + 2] * kernel[5] +
                        srcCenter[idx + stride - 4 + 2] * kernel[6] + srcCenter[idx + stride + 2] * kernel[7] + srcCenter[idx + stride + 4 + 2] * kernel[8];

                    dstCenter[idx] = (byte)Math.Clamp(sumB, 0f, 255f);
                    dstCenter[idx + 1] = (byte)Math.Clamp(sumG, 0f, 255f);
                    dstCenter[idx + 2] = (byte)Math.Clamp(sumR, 0f, 255f);
                    dstCenter[idx + 3] = srcCenter[idx + 3]; // Preserve Alpha
                }
            }
        }
    }
}

