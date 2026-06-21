using System.Runtime.CompilerServices;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{

    // Mixes two pixel buffers into one based on transition mode, pivot, and hardness.
    // Writes into the caller-provided result buffer (fully overwritten, every pixel) so no
    // per-recalc allocation occurs.
    public void MixSmooth(
        byte[] pixels1,
        byte[] pixels2,
        byte[] result)
    {
        if (pixels1.Length != pixels2.Length)
            throw new ArgumentException("Pixel arrays must have same length.");

        Hardness = Math.Clamp(Hardness, 0.0f, 1.0f);
        Pivot = Math.Clamp(Pivot, 0.0f, 1.0f);
        Widening = Math.Clamp(Widening, 0.0f, 1.0f);

        float lower = Pivot * Hardness;
        float upper = 1.0f - (1.0f - Pivot) * Hardness;
        bool isHardCut = (upper <= lower + 0.00001f);

        int stride = Width * TRANSITIONS_BPP;

        unsafe
        {
            fixed (byte* p1Start = pixels1)
            fixed (byte* p2Start = pixels2)
            fixed (byte* pResStart = result)
            {
                for (int y = 0; y < Height; y++)
                {
                    byte* row1 = p1Start + y * stride;
                    byte* row2 = p2Start + y * stride;
                    byte* rowR = pResStart + y * stride;

                    float ny = (float)y / (Height - 1);

                    for (int x = 0; x < Width; x++)
                    {
                        float nx = (float)x / (Width - 1);

                        float weight = ComputeWeight(Direction, Pivot, Widening, Shift, lower, upper, isHardCut, nx, ny);

                        byte* px1 = row1 + x * TRANSITIONS_BPP;
                        byte* px2 = row2 + x * TRANSITIONS_BPP;
                        byte* pxR = rowR + x * TRANSITIONS_BPP;

                        for (int b = 0; b < TRANSITIONS_BPP; b++)
                        {
                            pxR[b] = (byte)(px2[b] * (1.0f - weight) + px1[b] * weight);
                        }
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Computes the blend weight for one normalized pixel position.
    private float ComputeWeight(TransitionDirection mode, float pivot, float widening, float shift, float lower, float upper, bool isHardCut, float nx, float ny)
    {
        // 1. Compute the base V field (native 0.0 to 1.0 field)
        float focus = ComputeFocus(mode, nx, ny, widening, shift);

        // 2. Apply pivot and hardness
        float weight;
        if (isHardCut)
        {
            weight = focus >= pivot ? 1.0f : 0.0f;
        }
        else
        {
            // Clamping ensures the value stays within 0 and 1
            weight = Math.Clamp((focus - lower) / (upper - lower), 0.0f, 1.0f);
        }

        return weight;
    }
}