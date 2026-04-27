using System.Runtime.CompilerServices;

namespace TgaBuilderLib.Transitions
{
    public partial class TransitionHelper
    {

        // Mixes two pixel buffers into one based on transition mode, pivot, and hardness.
        public byte[] MixPixels(
            byte[] pixels1,
            byte[] pixels2)
        {
            if (pixels1.Length != pixels2.Length)
                throw new ArgumentException("Pixel arrays must have same length.");

            Hardness = Math.Clamp(Hardness, 0.0f, 1.0f);
            Pivot = Math.Clamp(Pivot, 0.0f, 1.0f);
            Offset = Math.Clamp(Offset, 0.0f, 1.0f);

            byte[] result = new byte[pixels1.Length];

            // When offset covers the entire texture, return pixels2 directly.
            if (Offset >= 1.0f)
            {
                Array.Copy(pixels2, result, result.Length);
                return result;
            }

            float lower = Pivot * Hardness;
            float upper = 1.0f - (1.0f - Pivot) * Hardness;
            bool isHardCut = (upper <= lower + 0.00001f);
            float offset = Offset;

            unsafe
            {
                fixed (byte* p1Start = pixels1)
                fixed (byte* p2Start = pixels2)
                fixed (byte* pResStart = result)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        byte* row1 = p1Start + y * Stride;
                        byte* row2 = p2Start + y * Stride;
                        byte* rowR = pResStart + y * Stride;

                        float ny = (float)y / (Height - 1);

                        for (int x = 0; x < Width; x++)
                        {
                            float nx = (float)x / (Width - 1);

                            float weight = IsInOffsetZone(Mode, nx, ny, offset)
                                ? 1.0f
                                : ComputeWeight(Mode, Pivot, lower, upper, isHardCut, nx, ny);

                            byte* px1 = row1 + x * TRANSITIONS_BPP;
                            byte* px2 = row2 + x * TRANSITIONS_BPP;
                            byte* pxR = rowR + x * TRANSITIONS_BPP;

                            for (int b = 0; b < TRANSITIONS_BPP; b++)
                            {
                                pxR[b] = (byte)(px1[b] * (1.0f - weight) + px2[b] * weight);
                            }
                        }
                    }
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Returns true when the pixel at (nx, ny) falls inside the pure-T2 offset zone.
        private static bool IsInOffsetZone(TransitionMode mode, float nx, float ny, float offset)
        {
            if (offset <= 0f) return false;
            return mode switch
            {
                TransitionMode.Top             => ny < offset,
                TransitionMode.Bottom          => ny > 1f - offset,
                TransitionMode.Left            => nx < offset,
                TransitionMode.Right           => nx > 1f - offset,
                TransitionMode.DiagonalTopLeft => nx < offset && ny < offset,
                TransitionMode.DiagonalTopRight => nx > 1f - offset && ny < offset,
                _                              => false
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Computes the blend weight for one normalized pixel position.
        private  float ComputeWeight(TransitionMode mode, float pivot, float lower, float upper, bool isHardCut, float nx, float ny)
        {

            (float distToT1, float distToT2) = ComputeTopologicy(mode, nx, ny);

            // 1. Compute the base V field (native 0.0 to 1.0 field)
            float v;
            if (distToT2 <= 0.00001f)
                v = 1.0f; // Strict boundary condition: we are on a texture-2 edge
            else if (distToT1 <= 0.00001f)
                v = 0.0f; // Strict boundary condition: we are on a texture-1 edge
            else
                v = distToT1 / (distToT1 + distToT2); // Smooth gradient in between

            // 2. Apply pivot and hardness
            float weight;
            if (isHardCut)
            {
                weight = v >= pivot ? 1.0f : 0.0f;
            }
            else
            {
                // Clamping ensures the value stays within 0 and 1
                weight = Math.Clamp((v - lower) / (upper - lower), 0.0f, 1.0f);
            }

            return weight;
        }
    }
}