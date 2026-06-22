using System.Runtime.CompilerServices;

namespace TrLynxLib.Transitions;

public partial class TransitionHelper
{
    private unsafe void BoxBlurGray(float[] blur, float[] gray)
    {
        int width = Width;
        int height = Height;

        fixed (float* pBlur = blur, pGray = gray)
        {
            for (int y = 1; y < height - 1; y++)
            {
                int row = y * width;

                // Set pointers to the start of the current row, row above, and row below
                float* pCurr = pGray + row;
                float* pPrev = pCurr - width;
                float* pNext = pCurr + width;
                float* pOut = pBlur + row;

                for (int x = 1; x < width - 1; x++)
                {
                    // Accumulate using simple relative pointer offsets
                    pOut[x] = (
                        pPrev[x - 1] + pPrev[x] + pPrev[x + 1] +
                        pCurr[x - 1] + pCurr[x] + pCurr[x + 1] +
                        pNext[x - 1] + pNext[x] + pNext[x + 1]
                    ) * 0.11111111f; // Multiplying by (1/9) is slightly faster than dividing
                }
            }
        }
    }

    private unsafe void MedianFilter3x3Gray(float[] output, float[] input)
    {
        int width = Width;
        int height = Height;

        fixed (float* pIn = input, pOut = output)
        {
            for (int y = 1; y < height - 1; y++)
            {
                int row = y * width;
                float* pCurr = pIn + row;
                float* pPrev = pCurr - width;
                float* pNext = pCurr + width;
                float* pDst = pOut + row;

                for (int x = 1; x < width - 1; x++)
                {
                    // Pull values directly into local stack variables
                    float v0 = pPrev[x - 1], v1 = pPrev[x], v2 = pPrev[x + 1];
                    float v3 = pCurr[x - 1], v4 = pCurr[x], v5 = pCurr[x + 1];
                    float v6 = pNext[x - 1], v7 = pNext[x], v8 = pNext[x + 1];

                    // Properly inlined C# local function for conditional swapping
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    void Swap(ref float a, ref float b)
                    {
                        if (a > b)
                        {
                            float t = a;
                            a = b;
                            b = t;
                        }
                    }

                    // Sorting network to extract the median into v4
                    Swap(ref v1, ref v2); Swap(ref v4, ref v5); Swap(ref v7, ref v8);
                    Swap(ref v0, ref v1); Swap(ref v3, ref v4); Swap(ref v6, ref v7);
                    Swap(ref v1, ref v2); Swap(ref v4, ref v5); Swap(ref v7, ref v8);
                    Swap(ref v0, ref v3); Swap(ref v5, ref v8); Swap(ref v4, ref v7);
                    Swap(ref v3, ref v6); Swap(ref v1, ref v4); Swap(ref v2, ref v5);
                    Swap(ref v4, ref v7); Swap(ref v4, ref v2); Swap(ref v6, ref v4);
                    Swap(ref v4, ref v2);

                    pDst[x] = v4;
                }
            }
        }
    }

    private unsafe void BilateralFilter3x3Gray(float[] output, float[] input, float sigma = 0.1f)
    {
        float safeSigma = Math.Max(sigma, 0.001f);
        float twoSigmaSq = 2f * safeSigma * safeSigma;

        fixed (float* pIn = input, pOut = output)
        {
            int width = Width;
            int height = Height;


            int* offsets = stackalloc int[9] {
                -width - 1, -width, -width + 1,
                -1,          0,      1,
                 width - 1,  width,  width + 1
            };

            for (int y = 1; y < height - 1; y++)
            {
                int rowOffset = y * width;
                float* srcCenter = pIn + rowOffset;
                float* dstCenter = pOut + rowOffset;

                for (int x = 1; x < width - 1; x++)
                {
                    float centerVal = srcCenter[x];
                    float sumWeight = 0f;
                    float sumVal = 0f;

                    for (int i = 0; i < 9; i++)
                    {
                        float neighborVal = srcCenter[x + offsets[i]];
                        float diff = centerVal - neighborVal;

                        float weight = (float)Math.Exp(-(diff * diff) / twoSigmaSq);

                        sumVal += neighborVal * weight;
                        sumWeight += weight;
                    }

                    dstCenter[x] = sumVal / sumWeight;
                }
            }
        }
    }

    private unsafe void GaussianBlur3x3Gray(float[] output, float[] input, float sigma = 1.0f)
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

        fixed (float* pIn = input, pOut = output)
        {
            int width = Width;
            int height = Height;

            for (int y = 1; y < height - 1; y++)
            {
                int rowOffset = y * width;

                float* srcCenter = pIn + rowOffset;
                float* dstCenter = pOut + rowOffset;

                for (int x = 1; x < width - 1; x++)
                {
                    float sum =
                        srcCenter[x - width - 1] * kernel[0] + srcCenter[x - width] * kernel[1] + srcCenter[x - width + 1] * kernel[2] +
                        srcCenter[x - 1] * kernel[3] + srcCenter[x] * kernel[4] + srcCenter[x + 1] * kernel[5] +
                        srcCenter[x + width - 1] * kernel[6] + srcCenter[x + width] * kernel[7] + srcCenter[x + width + 1] * kernel[8];

                    dstCenter[x] = sum;
                }
            }
        }
    }
}

