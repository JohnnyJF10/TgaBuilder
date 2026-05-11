using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    /// <summary>
    /// The ComputeFocus method is designed to calculate a distance-based gradient for texture transitions
    /// within a 2D coordinate system (0 to 1). It is primarily used to create blend weights between
    /// different textures or to exclude tiles from drawing if they do not meet certain threshold conditions.
    /// The function determines how far a given normalized point
    /// is from the transition's center or edges, effectively shaping the "profile" of the transition.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ComputeFocus(TransitionMode mode, float normalizedX, float normalizedY)
    {
        float distToT1 = 0, distToT2 = 0;
        const float epsilon = 0.000001f;

        // Clamping for safety
        Shift = Math.Clamp(Shift, -1.0f, 1.0f);

        if (mode <= TransitionMode.Left)
        {
            // ==========================================
            // ORTHOGONAL CASES
            // ==========================================
            float wideningInverse = Widening >= 1.0f ? 1e6f : 1.0f / (1.0f - Widening);

            if (mode == TransitionMode.Top || mode == TransitionMode.Bottom)
            {
                // Calculate side distance with explicit edge handling for Shift
                float sideDistance;
                if (Shift >= 1.0f - epsilon)
                    sideDistance = normalizedX * 0.5f; // Peak is at the right edge
                else if (Shift <= -1.0f + epsilon)
                    sideDistance = (1.0f - normalizedX) * 0.5f; // Peak is at the left edge
                else
                    sideDistance = Math.Min(normalizedX / (1.0f + Shift), (1.0f - normalizedX) / (1.0f - Shift));

                float distanceX = Widening >= 1.0f ? 0.5f : Math.Min(sideDistance * wideningInverse, 0.5f);

                (distToT1, distToT2) = mode == TransitionMode.Top
                    ? (Math.Min(distanceX, 1.0f - normalizedY), normalizedY)
                    : (Math.Min(distanceX, normalizedY), 1.0f - normalizedY);
            }
            else // Left or Right
            {
                float sideDistance;
                if (Shift >= 1.0f - epsilon)
                    sideDistance = normalizedY * 0.5f; // Peak is at the bottom edge
                else if (Shift <= -1.0f + epsilon)
                    sideDistance = (1.0f - normalizedY) * 0.5f; // Peak is at the top edge
                else
                    sideDistance = Math.Min(normalizedY / (1.0f + Shift), (1.0f - normalizedY) / (1.0f - Shift));

                float distanceY = Widening >= 1.0f ? 0.5f : Math.Min(sideDistance * wideningInverse, 0.5f);

                (distToT1, distToT2) = mode == TransitionMode.Left
                    ? (Math.Min(distanceY, 1.0f - normalizedX), normalizedX)
                    : (Math.Min(distanceY, normalizedX), 1.0f - normalizedX);
            }
        }
        else
        {
            // ==========================================
            // DIAGONAL CASES
            // ==========================================
            float coordA = normalizedX;
            float coordB = mode == TransitionMode.DiagonalTopLeft ? normalizedY : 1.0f - normalizedY;
            float averageCoord = (coordA + coordB) * 0.5f;
            float crossSectionWidth = (coordA + coordB) <= 1.0f ? (coordA + coordB) : (2.0f - (coordA + coordB));

            float coordANew, coordBNew;
            if (crossSectionWidth < 0.0001f)
            {
                coordANew = coordA;
                coordBNew = coordB;
            }
            else
            {
                float crossSectionPosition = (coordA - coordB) / crossSectionWidth;
                float shiftedCenter = Shift;

                float normalizedShiftedDistance;
                bool isRightOfCenter = crossSectionPosition >= shiftedCenter;

                if (isRightOfCenter)
                {
                    float availableSpace = 1.0f - shiftedCenter;
                    // FIX: If availableSpace is 0, we are AT the center/peak. 
                    // Return 0.0 distance to ensure it's treated as the plateau.
                    normalizedShiftedDistance = availableSpace < epsilon ? 0.0f : (crossSectionPosition - shiftedCenter) / availableSpace;
                }
                else
                {
                    float availableSpace = shiftedCenter - (-1.0f);
                    // FIX: Same logic for the left side
                    normalizedShiftedDistance = availableSpace < epsilon ? 0.0f : (shiftedCenter - crossSectionPosition) / availableSpace;
                }

                normalizedShiftedDistance = Math.Clamp(normalizedShiftedDistance, 0.0f, 1.0f);

                float widenedDistance = Widening >= 1.0f
                    ? 0.0f
                    : Math.Max(0.0f, normalizedShiftedDistance - Widening) / (1.0f - Widening);

                float newCrossSectionPosition = isRightOfCenter ? widenedDistance : -widenedDistance;
                float newDifference = newCrossSectionPosition * crossSectionWidth;
                float offsetFromAverage = newDifference * 0.5f;

                coordANew = averageCoord + offsetFromAverage;
                coordBNew = averageCoord - offsetFromAverage;
            }

            (distToT1, distToT2) = mode == TransitionMode.DiagonalTopLeft
                ? (Math.Min(1.0f - coordANew, 1.0f - coordBNew), Math.Min(coordANew, coordBNew))
                : (Math.Min(coordANew, coordBNew), Math.Min(1.0f - coordANew, 1.0f - coordBNew));
        }

        // Final Blend Logic
        if (distToT2 <= 0.00001f) return 1.0f;
        if (distToT1 <= 0.00001f) return 0.0f;
        return distToT1 / (distToT1 + distToT2);
    }
}

