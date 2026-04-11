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

            byte[] result = new byte[pixels1.Length];

            float lower = Pivot * Hardness;
            float upper = 1.0f - (1.0f - Pivot) * Hardness;
            bool isHardCut = (upper <= lower + 0.00001f);

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

                            float weight = ComputeWeight(Mode, Pivot, lower, upper, isHardCut, nx, ny);

                            byte* px1 = row1 + x * Bpp;
                            byte* px2 = row2 + x * Bpp;
                            byte* pxR = rowR + x * Bpp;

                            for (int b = 0; b < Bpp; b++)
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
        // Computes the blend weight for one normalized pixel position.
        private  float ComputeWeight(TransitionMode mode, float pivot, float lower, float upper, bool isHardCut, float nx, float ny)
        {
            float distToT1 = 0; // Distance to edges that must be 100% texture 1
            float distToT2 = 0; // Distance to edges that must be 100% texture 2

            switch (mode)
            {
                case TransitionMode.Top:
                    distToT2 = ny; // Texture 2 controls the top side (y=0)
                    distToT1 = Math.Min(nx, Math.Min(1.0f - nx, 1.0f - ny)); // Texture 1 controls left, right, and bottom
                    break;

                case TransitionMode.Bottom:
                    distToT2 = 1.0f - ny;
                    distToT1 = Math.Min(nx, Math.Min(1.0f - nx, ny));
                    break;

                case TransitionMode.Left:
                    distToT2 = nx;
                    distToT1 = Math.Min(ny, Math.Min(1.0f - ny, 1.0f - nx));
                    break;

                case TransitionMode.Right:
                    distToT2 = 1.0f - nx;
                    distToT1 = Math.Min(ny, Math.Min(1.0f - ny, nx));
                    break;

                case TransitionMode.DiagonalTopLeft:
                    distToT2 = Math.Min(nx, ny); // Top and left belong to texture 2
                    distToT1 = Math.Min(1.0f - nx, 1.0f - ny); // Bottom and right belong to texture 1
                    break;

                case TransitionMode.DiagonalTopRight:
                    distToT2 = Math.Min(1.0f - nx, ny); // Top and right belong to texture 2
                    distToT1 = Math.Min(nx, 1.0f - ny); // Bottom and left belong to texture 1
                    break;
            }

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