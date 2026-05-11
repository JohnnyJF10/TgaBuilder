using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    private unsafe void BoxBlur(float[] blur, float[] gray)
    {
        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                blur[idx] =
                    (gray[idx - Width - 1] + gray[idx - Width] + gray[idx - Width + 1] +
                     gray[idx - 1] + gray[idx] + gray[idx + 1] +
                     gray[idx + Width - 1] + gray[idx + Width] + gray[idx + Width + 1]) / 9f;
            }
        }
    }

    private void MedianFilter3x3(float[] output, float[] input)
    {
        float[] window = new float[9];
        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                window[0] = input[idx - Width - 1]; window[1] = input[idx - Width]; window[2] = input[idx - Width + 1];
                window[3] = input[idx - 1]; window[4] = input[idx]; window[5] = input[idx + 1];
                window[6] = input[idx + Width - 1]; window[7] = input[idx + Width]; window[8] = input[idx + Width + 1];

                for (int i = 1; i < 9; i++)
                {
                    float temp = window[i];
                    int j = i - 1;
                    while (j >= 0 && window[j] > temp)
                    {
                        window[j + 1] = window[j];
                        j--;
                    }
                    window[j + 1] = temp;
                }
                output[idx] = window[4]; //Median
            }
        }
    }

    private void MinFilter3x3(float[] output, float[] input)
    {
        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                float min = input[idx];

                if (input[idx - Width - 1] < min) min = input[idx - Width - 1];
                if (input[idx - Width] < min) min = input[idx - Width];
                if (input[idx - Width + 1] < min) min = input[idx - Width + 1];
                if (input[idx - 1] < min) min = input[idx - 1];
                if (input[idx + 1] < min) min = input[idx + 1];
                if (input[idx + Width - 1] < min) min = input[idx + Width - 1];
                if (input[idx + Width] < min) min = input[idx + Width];
                if (input[idx + Width + 1] < min) min = input[idx + Width + 1];

                output[idx] = min;
            }
        }
    }

    private void MaxFilter3x3(float[] output, float[] input)
    {
        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                float max = input[idx];

                if (input[idx - Width - 1] > max) max = input[idx - Width - 1];
                if (input[idx - Width] > max) max = input[idx - Width];
                if (input[idx - Width + 1] > max) max = input[idx - Width + 1];
                if (input[idx - 1] > max) max = input[idx - 1];
                if (input[idx + 1] > max) max = input[idx + 1];
                if (input[idx + Width - 1] > max) max = input[idx + Width - 1];
                if (input[idx + Width] > max) max = input[idx + Width];
                if (input[idx + Width + 1] > max) max = input[idx + Width + 1];

                output[idx] = max;
            }
        }
    }

    private void BilateralFilter3x3(float[] output, float[] input, float intensitySigma = 25f)
    {
        float sigmaSq = intensitySigma * intensitySigma * 2f;

        for (int y = 1; y < Height - 1; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                int idx = row + x;
                float centerVal = input[idx];
                float sumWeight = 0;
                float sumVal = 0;

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        float neighborVal = input[idx + (i * Width) + j];
                        float diff = centerVal - neighborVal;

                        float weight = (float)Math.Exp(-(diff * diff) / sigmaSq);

                        sumVal += neighborVal * weight;
                        sumWeight += weight;
                    }
                }
                output[idx] = sumVal / sumWeight;
            }
        }
    }
}

